using LabWork.Core;

namespace LabWork
{
    public class LabWork
    {

        public static void Main(string[] args)
        {
            var (components, stateSpaceModel, initialConditions, selectedOutputVars, timeStep, totalTime) = SetUp();

            var solver = new EulerSolver();

            string outputDir = "results";
            var visualizer = new ScottPlotVisualizer(outputDir);
            var visualizationData = new VisualizationData();

            solver.Solve(components, stateSpaceModel, initialConditions, timeStep, totalTime, visualizationData);

            visualizer.SaveIndividualGraphs(selectedOutputVars, visualizationData);
            Console.WriteLine($"\nРасчет завершен. Графики сохранены в папку: {Path.GetFullPath(outputDir)}");
        }

        private static (List<Component>, StateSpaceModel, Dictionary<string, double>, List<string>, double, double) SetUp()
        {
            var allComponents = CreateCircuitFromUserInput();

            if (allComponents.Count == 0)
            {
                Console.WriteLine("Схема не содержит компонентов. Завершение работы.");
                Environment.Exit(0); // Корректно завершаем программу
            }

            Console.WriteLine("--- Шаг 2: Построение дерева и определение хорд ---");
            var treeBuilder = new TreeBuilder();
            var (treeBranches, chords) = treeBuilder.BuildTree(allComponents);
            Console.WriteLine("Дерево построено. Ветви дерева: " + string.Join(", ", treeBranches.Select(c => c.Name)));
            Console.WriteLine("Хорды определены: " + string.Join(", ", chords.Select(c => c.Name)));


            // ПОСТРОЕНИЕ М-МАТРИЦЫ
            Console.WriteLine("\n--- Шаг 3: Построение М-матрицы ---");
            var matrixBuilder = new MatrixMBuilder();
            var (mMatrix, rowLabels, columnLabels) = matrixBuilder.BuildMatrix(treeBranches, chords);
            Console.WriteLine("М-матрица успешно сгенерирована.");


            // АНАЛИЗ СХЕМЫ
            Console.WriteLine("\n--- Шаги 4-5: Формирование модели пространства состояний ---");
            var parser = new MatrixMParser(allComponents, mMatrix, rowLabels, columnLabels);
            var (coefficientMatrix, variableOrder) = parser.Parse();

            var analyzer = new CircuitAnalyzer(allComponents, coefficientMatrix, variableOrder);
            var stateSpaceModel = analyzer.BuildStateSpaceModel();
            Console.WriteLine("Модель пространства состояний построена.");

            // НАСТРОЙКА И ЗАПУСК
            Console.WriteLine("\n=== ВВОД НАЧАЛЬНЫХ УСЛОВИЙ (при t = 0) ===");
            var initialConditions = new Dictionary<string, double>();

            foreach (var stateVar in stateSpaceModel.StateVariables)
            {
                Console.Write($"Введите начальное значение для {stateVar}(0): ");
                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    // для тестинга
                    if (stateVar == "U_C") initialConditions[stateVar] = 10; 
                    else if (stateVar == "I_L") initialConditions[stateVar] = 0;
                    else initialConditions[stateVar] = 0;

                    Console.WriteLine($"Использовано значение по умолчанию: {initialConditions[stateVar]}");
                }
                else if (double.TryParse(line, out double value))
                {
                    initialConditions[stateVar] = value;
                }
                else
                {
                    Console.WriteLine($"Неверный формат, установлено значение по умолчанию 0 для {stateVar}");
                    initialConditions[stateVar] = 0;
                }
            }
            
            Console.WriteLine("\n=== ВЫБОР ВЫХОДНЫХ ПЕРЕМЕННЫХ ДЛЯ НАБЛЮДЕНИЯ ===");
            Console.WriteLine("Доступные переменные для вывода:");
            for (int i = 0; i < variableOrder.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {variableOrder[i]}");
            }
            Console.Write("Введите номера переменных через запятую (например, 3,4) или оставьте пустым для вывода всех: ");
            string input = Console.ReadLine();
            var selectedOutputVars = new List<string>();

            if (string.IsNullOrWhiteSpace(input))
            {
                selectedOutputVars = variableOrder; // Выводим все
            }
            else
            {
                var indices = input.Split(',').Select(s => s.Trim()).Where(s => int.TryParse(s, out _)).Select(int.Parse).ToList();
                foreach (int idx in indices)
                {
                    if (idx >= 1 && idx <= variableOrder.Count)
                        selectedOutputVars.Add(variableOrder[idx - 1]);
                }
            }

            analyzer.SetOutputVariables(stateSpaceModel, selectedOutputVars);

            // === ВВОД ПАРАМЕТРОВ ВРЕМЕНИ ===
            Console.WriteLine("\n=== ПАРАМЕТРЫ МОДЕЛИРОВАНИЯ ===");
            
            Console.Write("Введите шаг по времени (по умолчанию 0.0001): ");
            string timeStepInput = Console.ReadLine();
            double timeStep = 0.0001;
            if (!string.IsNullOrWhiteSpace(timeStepInput) && double.TryParse(timeStepInput, out double ts))
            {
                timeStep = ts;
            }
            else
            {
                Console.WriteLine($"Использовано значение по умолчанию: {timeStep}");
            }

            Console.Write("Введите общее время моделирования (по умолчанию 0.1): ");
            string totalTimeInput = Console.ReadLine();
            double totalTime = 0.1;
            if (!string.IsNullOrWhiteSpace(totalTimeInput) && double.TryParse(totalTimeInput, out double tt))
            {
                totalTime = tt;
            }
            else
            {
                Console.WriteLine($"Использовано значение по умолчанию: {totalTime}");
            }

            return (allComponents, stateSpaceModel, initialConditions, selectedOutputVars, timeStep, totalTime);
        }

        private static List<Component> CreateCircuitFromUserInput()
        {
            Console.Clear();
            Console.WriteLine("=== Конструктор электрической схемы ===");
            Console.WriteLine("Для завершения введите 'готово' или 'x'.");
            Console.WriteLine("Доступные типы компонентов: R, C, L, E (источник ЭДС), J (источник тока).");
            Console.WriteLine("-----------------------------------------");

            var components = new List<Component>();
            int componentCount = 1;

            while (true)
            {
                Console.WriteLine($"\n--- Добавление компонента #{componentCount} ---");

                // 1. Ввод имени
                string name;
                bool finishInput = false;

                while (true)
                {
                    Console.Write("Введите имя компонента (например, R1, C1) или 'x' для выхода: ");
                    string rawInput = Console.ReadLine();
                    
                    // сразу проверку на выход
                    if (rawInput != null && (rawInput.Trim().ToLower() == "готово" || rawInput.Trim().ToLower() == "x"))
                    {
                        finishInput = true;
                        name = "";
                        break;
                    }

                    name = rawInput;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Имя не может быть пустым");
                        continue;
                    }
                    if (components.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine("Компонент с таким именем уже существует");
                        continue;
                    }
                    break;
                }

                if (finishInput) break;

                // 2. Ввод типа
                string type;
                var validTypes = new List<string> { "R", "C", "L", "E", "J", "G" };
                while (true)
                {
                    Console.Write("Введите тип компонента (R, C, L, E, J, G): ");
                    type = Console.ReadLine().ToUpper();
                    if (validTypes.Contains(type)) break;
                    Console.WriteLine("Неверный тип");
                }

                // 3. Ввод значения
                double value;
                while (true)
                {
                    string prompt;
                    if (type == "G")
                    {
                        prompt = $"Введите S для {name}: ";
                    }
                    else
                    {
                        prompt = $"Введите значение для {name} (Ом, Ф, Гн, В, А): ";
                    }
                    Console.Write(prompt);
                    if (double.TryParse(Console.ReadLine(), out value)) break;
                    Console.WriteLine("Введите корректное число");
                }
                
                string controlComponentName = "";
                if (type == "G")
                {
                    Console.Write($"Введите название компонента, от которого зависит сила тока на управляемом источнике {name}: ");
                    controlComponentName = Console.ReadLine();
                    Console.WriteLine();
                }

                // 4. Ввод узлов
                int node1, node2;
                while (true)
                {
                    Console.Write("Введите номер первого узла (целое число, например, 1): ");
                    if (int.TryParse(Console.ReadLine(), out node1)) break;
                    Console.WriteLine("Введите корректное целое число.");
                }
                while (true)
                {
                    Console.Write("Введите номер второго узла: ");
                    if (int.TryParse(Console.ReadLine(), out node2) && node2 != node1) break;
                    Console.WriteLine("Введите целое число, не совпадающее с первым узлом");
                }


                int? cNode1 = null;
                int? cNode2 = null;

                if (type == "G")
                {
                    Console.WriteLine($"Настройка управления для {name}");
                    while (true)
                    {
                        Console.Write("Введите номер узла 'плюс' управляющего напряжения: ");
                        if (int.TryParse(Console.ReadLine(), out int res))
                        {
                            cNode1 = res;
                            break;
                        }
                    }
                    while (true)
                    {
                        Console.Write("Введите номер узла 'минус' управляющего напряжения: ");
                        if (int.TryParse(Console.ReadLine(), out int res) && res != cNode1)
                        {
                            cNode2 = res;
                            break;
                        }
                    }
                }

                Component newComponent;
                if (type == "G")
                {
                    newComponent = new ControlledCurrentSource();
                    ((ControlledCurrentSource) newComponent).ControlComponentName = controlComponentName;
                } 
                else
                {
                    newComponent = new Component();
                }
                newComponent.Name = name;
                newComponent.Type = type;
                newComponent.Value = value;
                newComponent.Node1 = node1;
                newComponent.Node2 = node2;
                newComponent.ControlNode1 = cNode1;
                newComponent.ControlNode2 = cNode2;
                
                components.Add(newComponent);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Добавлен: {newComponent.Name} ({newComponent.Type}) между узлами {newComponent.Node1} и {newComponent.Node2}");
                Console.ResetColor();

                componentCount++;
            }

            Console.WriteLine("\n-----------------------------------------");
            Console.WriteLine("Ввод схемы завершен. Собрана схема:");
            foreach (var component in components)
            {
                Console.WriteLine($" - {component}");
            }
            Console.WriteLine("-----------------------------------------");

            return components;
        }
    }
}