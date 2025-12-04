//using System.Numerics;
using LabWork.models;

namespace LabWork
{
    public class VisualizationData
    {   
        private List<double> _timeHistory = new List<double>();
        private List<Vector> _yHistory = new List<Vector>();

        public int SeriesCount => _yHistory.Count > 0 ? _yHistory[0].Size : 0;

        public IReadOnlyList<double> TimeHistory => _timeHistory.AsReadOnly();
        //public IReadOnlyList<double[]> YHistory => _yHistory.AsReadOnly();
        public double[] GetYSeries(int yIndex)
        {
            return _yHistory.Select(y => y[yIndex]).ToArray();
        }

        public void AddDataPoint(double time, Vector y)
        {
            _timeHistory.Add(time);
            _yHistory.Add(y);
        }
    }
}
