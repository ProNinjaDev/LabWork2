public class LinearSystemAnalyzer
{
    private const double Tolerance = 1e-10;
    public List<string> IndependentVariables { get; set; } = new List<string>();
    public List<string> DependentVariables { get; set; } = new List<string>();
    public void FindIndependentVariables(double[][] matrix, List<string> variableNames)
    {
        int rows = matrix.Length;
        int cols = matrix[0].Length;
        int currentRow = 0;
        while (currentRow < rows)
        {
            for (int i = currentRow; i < rows; i++)
            {
                if (Math.Abs(matrix[i][currentRow]) > Tolerance)
                {
                    SwapRows(matrix, currentRow, i);
                }
            }
            for (int j = currentRow; j < cols; j++)
            {
                if (Math.Abs(matrix[currentRow][j]) > Tolerance)
                {
                    SwapCols(matrix, variableNames, currentRow, j);
                }
            }

            double value = matrix[currentRow][currentRow];
            if (Math.Abs(value) > Tolerance)
            {
                NormalizeRow(matrix, currentRow, value);

                for (int i = 0; i < rows; i++)
                {
                    if (i != currentRow)
                    {
                        double factor = matrix[i][currentRow];
                        for (int j = 0; j < cols; j++)
                        {
                            matrix[i][j] -= factor * matrix[currentRow][j];
                        }
                    }
                }
            }


            currentRow++;
        }

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write(string.Format("{0} ", matrix[i][j]));
            }
            Console.Write(Environment.NewLine + Environment.NewLine);
        }

        var independent = new List<string>();
        var dependent = new List<string>();

        int rank = 0;
        for (int i = 0; i < matrix.Length; i++)
        {
            if (i < matrix[0].Length && Math.Abs(matrix[i][i] - 1.0) < Tolerance)
            {
                dependent.Add(variableNames[i]);
                rank++;
            }
            else
            {
                break;
            }
        }

        for (int j = rank; j < variableNames.Count; j++)
        {
            independent.Add(variableNames[j]);
        }

        IndependentVariables = independent;
        DependentVariables = dependent;
    }
    static void SwapRows(double[][] matrix, int row1, int row2)
    {
        for (int j = 0; j < matrix[0].Length; j++)
        {
            double temp = matrix[row1][j];
            matrix[row1][j] = matrix[row2][j];
            matrix[row2][j] = temp;
        }
    }
    static void SwapCols(double[][] matrix, List<string> variableNames, int col1, int col2)
    {
        for (int i = 0; i < matrix.Length; i++)
        {
            double temp = matrix[i][col1];
            matrix[i][col1] = matrix[i][col2];
            matrix[i][col2] = temp;
        }
        string tmp = variableNames[col1];
        variableNames[col1] = variableNames[col2];
        variableNames[col2] = tmp;
    }
    static void NormalizeRow(double[][] matrix, int row, double value)
    {
        for (int j = 0; j < matrix[0].Length; j++)
        {
            matrix[row][j] /= value;
        }
    }
}