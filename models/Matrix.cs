using System;
using System.Collections.Generic;

namespace LabWork.models
{
    public class Matrix
    {
        private List<List<double>> _values;

        public Matrix(int rows, int cols)
        {
            _values = new List<List<double>>();
            for (int i = 0; i < rows; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < cols; j++)
                {
                    row.Add(0.0);
                }
                _values.Add(row);
            }
        }
        public Matrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            _values = new List<List<double>>();
            for (int i = 0; i < rows; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < cols; j++)
                {
                    row.Add(matrix[i, j]);
                }
                _values.Add(row);
            }
        }
        public int Rows => _values.Count;
        public int Cols => _values.Count > 0 ? _values[0].Count : 0;

        public double this[int row, int col]
        {
            get { return _values[row][col]; }
            set { _values[row][col] = value; }
        }

        public static Vector operator *(Matrix m, Vector v)
        {
            if (m.Cols != v.Size)
            {
                throw new ArgumentException("Кол-во столбцов матрицы должно быть равно размеру вектора.");
            }

            var resultVector = new Vector(m.Rows);
            for (int i = 0; i < m.Rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < m.Cols; j++)
                {
                    sum += m[i, j] * v[j];
                }
                resultVector[i] = sum;
            }
            return resultVector;
        }
    }
}