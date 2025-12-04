namespace LabWork;

public class CircuitAnalyzer
{
    private List<Component> components;
    private List<List<double>> coefficientMatrix;
    private List<string> variableOrder;
    private List<List<double>> reducedMatrix;
    private int stateAndInputVarsCount;

    public CircuitAnalyzer(List<Component> components, List<List<double>> coefficientMatrix, List<string> variableOrder)
    {
        this.components = components;
        this.coefficientMatrix = coefficientMatrix;
        this.variableOrder = variableOrder;
    }

    public StateSpaceModel BuildStateSpaceModel()
    {
        var (stateVars, inputVars) = RearrangeAndReduceMatrix();

        var model = BuildABMatrices(stateVars, inputVars);

        return model;
    }

    private (List<string>, List<string>) RearrangeAndReduceMatrix()
    {
        reducedMatrix = coefficientMatrix.Select(row => row.ToList()).ToList();

        var stateVars = new List<string>();
        var inputVars = new List<string>();
        var otherVars = new List<string>();

        foreach (var varName in variableOrder)
        {
            var parts = varName.Split('_');
            var componentType = parts[0];
            var componentName = parts[1];

            var comp = components.FirstOrDefault(c => c.Name == componentName);
            if (comp.Type == "C" && componentType == "U") stateVars.Add(varName);
            else if (comp.Type == "L" && componentType == "I") stateVars.Add(varName);
            else if ((componentType == "U" && comp.Type == "E") || (componentType == "I" && comp.Type == "J")) inputVars.Add(varName);
            else otherVars.Add(varName);
        }

        var newOrder = stateVars.Concat(inputVars).Concat(otherVars).ToList();

        var columnMapping = newOrder.Select(newVar => variableOrder.IndexOf(newVar)).ToList();
        var reorderedMatrix = new List<List<double>>();

        foreach (var row in reducedMatrix)
        {
            var newRow = new List<double>();
            foreach (int oldIndex in columnMapping)
            {
                newRow.Add(row[oldIndex]);
            }
            reorderedMatrix.Add(newRow);
        }

        reducedMatrix = reorderedMatrix;
        variableOrder = newOrder;

        List<List<double>> list = new List<List<double>>();
        List<string> variableNames = new List<string>();
        List<int> extendedRows = new List<int>();
        int currentRow = 0;
        while (currentRow < reducedMatrix.Count)
        {
            var row = reducedMatrix[currentRow];
            bool isAllNull = true;
            for (int col = stateVars.Count + inputVars.Count; col < row.Count; col++)
            {
                if (Math.Abs(row[col]) > 1e-10)
                {
                    isAllNull = false;
                    break;
                }
            }
            if (isAllNull)
            {
                extendedRows.Add(currentRow);
                List<double> r = new List<double>();
                for (int col = 0; col < stateVars.Count; col++)
                {
                    r.Add(row[col]);
                }
                list.Add(r);
            }

            currentRow++;
        }
        for (int col = 0; col < stateVars.Count; col++)
        {
            variableNames.Add(stateVars[col]);
        }
        double[][] matrix = [.. list.Select(innerList => innerList.ToArray())];

        LinearSystemAnalyzer analyzer = new LinearSystemAnalyzer();
        analyzer.FindIndependentVariables(matrix, variableNames);

        List<string> dependentVariables = analyzer.DependentVariables;
        foreach (var variable in dependentVariables)
        {
            int removeItemIndex = variableOrder.IndexOf(variable);

            string toRemove = stateVars[removeItemIndex];
            stateVars.RemoveAt(removeItemIndex);
            variableOrder.RemoveAt(removeItemIndex);

            otherVars.Add(toRemove);
            variableOrder.Add(toRemove);
            for (int i = 0; i < reducedMatrix.Count; i++)
            {
                var curRow = reducedMatrix[i];
                double value = curRow[removeItemIndex];
                curRow.RemoveAt(removeItemIndex);
                curRow.Add(value);
            }
        }

        foreach (var roww in extendedRows)
        {
            var r = reducedMatrix[roww];
            var equation = new double[variableOrder.Count].ToList();

            for (int i = 0; i < variableOrder.Count; i++)
            {
                var varName = variableOrder[i];
                var parts = varName.Split('_');
                var componentType = parts[0];
                var componentName = parts[1];

                var comp = components.FirstOrDefault(c => c.Name == componentName);
                if (comp.Type == "C" && componentType == "U")
                {
                    int idx = variableOrder.IndexOf($"I_{componentName}");
                    equation[idx] = r[i] / comp.Value;
                }
                if (comp.Type == "L" && componentType == "I")
                {
                    int idx = variableOrder.IndexOf($"U_{componentName}");
                    equation[idx] = r[i] / comp.Value;
                }
            }

            reducedMatrix.Add(equation);
        }

        this.stateAndInputVarsCount = stateVars.Count + inputVars.Count;

        GaussianElimination();

        return (stateVars, inputVars);
    }
    private void GaussianElimination()
    {
        int rows = reducedMatrix.Count;
        int cols = variableOrder.Count;
        int pivotRow = 0;

        for (int col = stateAndInputVarsCount; col < cols && pivotRow < rows; col++)
        {
            int nonZeroRow = -1;
            for (int row = pivotRow; row < rows; row++)
            {
                if (Math.Abs(reducedMatrix[row][col]) > 1e-10)
                {
                    nonZeroRow = row;
                    break;
                }
            }

            if (nonZeroRow != pivotRow)
            {
                var temp = reducedMatrix[pivotRow];
                reducedMatrix[pivotRow] = reducedMatrix[nonZeroRow];
                reducedMatrix[nonZeroRow] = temp;
            }

            double pivot = reducedMatrix[pivotRow][col];
            for (int j = 0; j < cols; j++)
            {
                reducedMatrix[pivotRow][j] /= pivot;
            }

            for (int row = 0; row < rows; row++)
            {
                if (row != pivotRow)
                {
                    double factor = reducedMatrix[row][col];
                    for (int j = 0; j < cols; j++)
                    {
                        reducedMatrix[row][j] -= factor * reducedMatrix[pivotRow][j];
                    }
                }
            }

            pivotRow++;
        }
    }
    private StateSpaceModel BuildABMatrices(List<string> stateVars, List<string> inputVars)
    {
        var model = new StateSpaceModel();

        var stateVarIndices = new List<int>();
        foreach (var var in stateVars)
        {
            int idx = variableOrder.IndexOf(var);
            stateVarIndices.Add(idx);
            model.StateVariables.Add(var);
        }

        var inputVarIndices = new List<int>();
        foreach (var var in inputVars)
        {
            int idx = variableOrder.IndexOf(var);
            inputVarIndices.Add(idx);
            model.InputVariables.Add(var);
        }

        int stateCount = stateVarIndices.Count;
        int inputCount = inputVarIndices.Count;

        model.A = new double[stateCount, stateCount];
        model.B = new double[stateCount, inputCount];

        // Для каждой переменной состояния находим соответствующее уравнение
        foreach (var stateVar in model.StateVariables)
        {
            int stateIndex = model.StateVariables.IndexOf(stateVar);
            var parts = stateVar.Split('_');
            var componentType = parts[0];
            var componentName = parts[1];

            var comp = components.First(c => c.Name == componentName);

            if (comp.Type == "C" && componentType == "U")
            {
                int iCIndex = variableOrder.IndexOf($"I_{componentName}");
                int equationRow = FindEquationForVariable(iCIndex);

                double capacitance = comp.Value;
                for (int j = 0; j < stateCount; j++)
                {
                    model.A[stateIndex, j] = -reducedMatrix[equationRow][stateVarIndices[j]] / capacitance;
                }
                for (int j = 0; j < inputCount; j++)
                {
                    model.B[stateIndex, j] = -reducedMatrix[equationRow][inputVarIndices[j]] / capacitance;
                }
            }
            else if (comp.Type == "L" && componentType == "I")
            {
                int uLIndex = variableOrder.IndexOf($"U_{componentName}");
                int equationRow = FindEquationForVariable(uLIndex);

                double inductance = comp.Value;
                for (int j = 0; j < stateCount; j++)
                {
                    model.A[stateIndex, j] = -reducedMatrix[equationRow][stateVarIndices[j]] / inductance;
                }
                for (int j = 0; j < inputCount; j++)
                {
                    model.B[stateIndex, j] = -reducedMatrix[equationRow][inputVarIndices[j]] / inductance;
                }
            }
        }

        return model;
    }
    private int FindEquationForVariable(int variableIndex)
    {
        for (int i = 0; i < reducedMatrix.Count; i++)
        {
            if (Math.Abs(Math.Abs(reducedMatrix[i][variableIndex]) - 1.0) < 1e-10)
            {
                return i;
            }
        }
        return -1;
    }
    public void PrintStateSpaceModel(StateSpaceModel model)
    {
        Console.WriteLine("\n=== МОДЕЛЬ В ПРОСТРАНСТВЕ СОСТОЯНИЙ ===");

        Console.WriteLine("\nПеременные состояния X:");
        Console.WriteLine(string.Join(", ", model.StateVariables));

        Console.WriteLine("\nВходные переменные V:");
        Console.WriteLine(string.Join(", ", model.InputVariables));

        Console.WriteLine("\nВыходные переменные Y:");
        Console.WriteLine(string.Join(", ", model.OutputVariables));

        Console.WriteLine("\nМатрица A:");
        PrintMatrix(model.A);

        Console.WriteLine("\nМатрица B:");
        PrintMatrix(model.B);

        if (model.C != null)
        {
            Console.WriteLine("\nМатрица C:");
            PrintMatrix(model.C);
        }

        if (model.D != null)
        {
            Console.WriteLine("\nМатрица D:");
            PrintMatrix(model.D);
        }
    }
    private void PrintMatrix(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write($"{matrix[i, j]:F4}\t");
            }
            Console.WriteLine();
        }
    }
    public void SetOutputVariables(StateSpaceModel model, List<string> outputVariables)
    {
        model.OutputVariables = outputVariables;
        int outputCount = outputVariables.Count;
        int stateCount = model.StateVariables.Count;
        int inputCount = model.InputVariables.Count;

        model.C = new double[outputCount, stateCount];
        model.D = new double[outputCount, inputCount];

        var stateVarIndices = model.StateVariables.Select(var => variableOrder.IndexOf(var)).ToList();
        var inputVarIndices = model.InputVariables.Select(var => variableOrder.IndexOf(var)).ToList();

        for (int i = 0; i < outputCount; i++)
        {
            string outputVar = outputVariables[i];

            int stateIndex = model.StateVariables.IndexOf(outputVar);
            if (stateIndex != -1)
            {
                model.C[i, stateIndex] = 1.0;
                continue;
            }

            int outputVarIndex = variableOrder.IndexOf(outputVar);

            if (outputVarIndex == -1) continue;

            int equationRow = FindEquationForVariable(outputVarIndex);

            for (int j = 0; j < stateCount; j++)
            {
                model.C[i, j] = -reducedMatrix[equationRow][stateVarIndices[j]];
            }

            for (int j = 0; j < inputCount; j++)
            {
                model.D[i, j] = -reducedMatrix[equationRow][inputVarIndices[j]];
            }
        }
    }
}