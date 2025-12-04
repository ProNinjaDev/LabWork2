namespace LabWork;

public class MatrixMParser
{
    public List<Component> Components { get; private set; }
    public List<List<double>> M { get; private set; }
    public List<string> RowLabels { get; private set; }
    public List<string> ColumnLabels { get; private set; }

    public MatrixMParser(List<Component> components, List<List<double>> m,
                        List<string> rowLabels, List<string> columnLabels)
    {
        Components = components;
        M = m;
        RowLabels = rowLabels;
        ColumnLabels = columnLabels;
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

        foreach (var resistor in Components.Where(c => c.Type == "R"))
        {
            var equation = CreateOhmLawEquation(resistor, variableOrder);
            coefficientMatrix.Add(equation);
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
}
