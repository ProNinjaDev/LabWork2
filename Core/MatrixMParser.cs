namespace LabWork;

public class MatrixMParser
{
    public List<Component> Components { get; private set; }
    public List<List<double>> M { get; private set; }
    public List<string> RowLabels { get; private set; }
    public List<string> ColumnLabels { get; private set; }

    private List<Component> _treeBranches; 

    public MatrixMParser(List<Component> components, List<List<double>> m,
                        List<string> rowLabels, List<string> columnLabels, List<Component> treeBranches)
    {
        Components = components;
        M = m;
        RowLabels = rowLabels;
        ColumnLabels = columnLabels;
        _treeBranches = treeBranches;
    }

    public (List<List<double>> coefficientMatrix, List<string> variableOrder) Parse()
    {
        var variableOrder = GenerateVariableOrder();
        var coefficientMatrix = new List<List<double>>();

        for (int i = 0; i < RowLabels.Count; i++)
        {
            var equation = CreateVoltageEquation(RowLabels[i], M[i], variableOrder);
            coefficientMatrix.Add(equation);
        }

        for (int j = 0; j < ColumnLabels.Count; j++)
        {
            var equation = CreateCurrentEquation(ColumnLabels[j], GetColumn(j), variableOrder);
            coefficientMatrix.Add(equation);
        }

        foreach (var comp in Components)
        {
            if (comp.Type == "R")
            {
                var equation = CreateOhmLawEquation(comp, variableOrder);
                coefficientMatrix.Add(equation);
            }
            else if (comp.Type == "G")
            {
                var equation = CreateVCCSEquation(comp, variableOrder);
                coefficientMatrix.Add(equation);
            }
        }

        return (coefficientMatrix, variableOrder);
    }

    private List<string> GenerateVariableOrder()
    {
        var variables = new List<string>();

        foreach (var comp in Components.OrderBy(c => c.Name))
        {
            variables.Add($"U_{comp.Name}");
            variables.Add($"I_{comp.Name}");
        }

        return variables;
    }

    private List<double> CreateVoltageEquation(string rowComponent, List<double> rowCoefficients, List<string> variableOrder)
    {
        var equation = new double[variableOrder.Count].ToList();

        var rowComp = Components.FirstOrDefault(c => c.Name == rowComponent);
        double rowSign = (rowComp?.Type == "E") ? -1 : 1;

        int rowVoltageIndex = variableOrder.IndexOf($"U_{rowComponent}");
        equation[rowVoltageIndex] = rowSign;

        for (int j = 0; j < ColumnLabels.Count; j++)
        {
            string colComponent = ColumnLabels[j];
            double coefficient = rowCoefficients[j];

            var colComp = Components.FirstOrDefault(c => c.Name == colComponent);
            double colSign = (colComp?.Type == "E") ? -1 : 1;

            int colVoltageIndex = variableOrder.IndexOf($"U_{colComponent}");
            equation[colVoltageIndex] = colSign * coefficient;
        }

        return equation;
    }

    private List<double> CreateCurrentEquation(string colComponent, List<double> columnCoefficients, List<string> variableOrder)
    {
        var equation = new double[variableOrder.Count].ToList();

        int colCurrentIndex = variableOrder.IndexOf($"I_{colComponent}");
        equation[colCurrentIndex] = 1;


        for (int i = 0; i < RowLabels.Count; i++)
        {
            string rowComponent = RowLabels[i];
            double coefficient = columnCoefficients[i];

            int rowCurrentIndex = variableOrder.IndexOf($"I_{rowComponent}");
            equation[rowCurrentIndex] = -coefficient;
        }

        return equation;
    }

    private List<double> CreateOhmLawEquation(Component resistor, List<string> variableOrder)
    {
        var equation = new double[variableOrder.Count].ToList();

        int voltageIndex = variableOrder.IndexOf($"U_{resistor.Name}");
        int currentIndex = variableOrder.IndexOf($"I_{resistor.Name}");

        equation[voltageIndex] = 1;
        equation[currentIndex] = -resistor.Value;

        return equation;
    }

    private List<double> GetColumn(int columnIndex)
    {
        return M.Select(row => row[columnIndex]).ToList();
    }

    private List<double> CreateVCCSEquation(Component gComp, List<string> variableOrder)
    {
        var equation = new double[variableOrder.Count].ToList();
        
        // коэф при токе самого источника  равен 1
        int currentIndex = variableOrder.IndexOf($"I_{gComp.Name}");
        equation[currentIndex] = 1.0;

        // заданы управляющие узлы - ищем путь в дереве
        if (gComp.ControlNode1.HasValue && gComp.ControlNode2.HasValue)
        {
            var path = FindPathInTree(gComp.ControlNode1.Value, gComp.ControlNode2.Value);
            
            double S = gComp.Value; // крутизна
            int currentNode = gComp.ControlNode1.Value;

            foreach (var branch in path)
            {
                int voltageIndex = variableOrder.IndexOf($"U_{branch.Name}");
                
                // если идем по стрелке ветви, то +1, иначе -1
                double sign = (branch.Node1 == currentNode) ? 1.0 : -1.0;
                
                equation[voltageIndex] -= S * sign;

                currentNode = (branch.Node1 == currentNode) ? branch.Node2 : branch.Node1;
            }
        }
        return equation;
    }

    private List<Component> FindPathInTree(int startNode, int endNode)
    {
        // строим граф деерва
        var adj = new Dictionary<int, List<(int, Component)>>();
        foreach(var b in _treeBranches) {
            if(!adj.ContainsKey(b.Node1)) adj[b.Node1] = new List<(int, Component)>();
            if(!adj.ContainsKey(b.Node2)) adj[b.Node2] = new List<(int, Component)>();
            adj[b.Node1].Add((b.Node2, b));
            adj[b.Node2].Add((b.Node1, b));
        }

        var queue = new Queue<int>();
        queue.Enqueue(startNode);
        var cameFrom = new Dictionary<int, (int p, Component b)>();
        cameFrom[startNode] = (-1, null);

        while(queue.Count > 0) {
            var curr = queue.Dequeue();
            if(curr == endNode) break;
            if (adj.ContainsKey(curr)) {
                foreach(var edge in adj[curr]) {
                    if(!cameFrom.ContainsKey(edge.Item1)) {
                        cameFrom[edge.Item1] = (curr, edge.Item2);
                        queue.Enqueue(edge.Item1);
                    }
                }
            }
        }

        if(!cameFrom.ContainsKey(endNode)) return new List<Component>();
        
        var path = new List<Component>();
        var cur = endNode;
        while(cur != startNode) {
            var step = cameFrom[cur];
            path.Add(step.b);
            cur = step.p;
        }
        path.Reverse();
        return path;
    }
}
        
    
