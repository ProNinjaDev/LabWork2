using System;
using System.Collections.Generic;


namespace LabWork.models
{
    public class Vector
    {
        private List<double> _values;

        public Vector(List<double> initialValues)
        {
            _values = new List<double>(initialValues);
        }

        public Vector(int size)
        {
            _values = new List<double>();
            for (int i = 0; i < size; i++)
            {
                _values.Add(0.0);
            }
        }

        public int Size => _values.Count;

        // Индексатор
        public double this[int index]
        {
            get { return _values[index]; }
            set { _values[index] = value; }
        }

        public static Vector operator +(Vector a, Vector b)
        {
            if (a.Size != b.Size)
            {
                throw new Exception("Размеры векторов для сложения должны совпадать");
            }

            var resultVector = new Vector(a.Size);

            for (int i = 0; i < a.Size; i++)
            {
                resultVector[i] = a[i] + b[i];
            }

            return resultVector;
        }

        public static Vector operator *(Vector v, double scalar)
        {
            var resultVector = new Vector(v.Size);
            for (int i = 0; i < v.Size; i++)
            {
                resultVector[i] = v[i] * scalar;
            }
            return resultVector;
        }
    }
}