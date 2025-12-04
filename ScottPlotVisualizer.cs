using ScottPlot;

namespace LabWork
{
    public class ScottPlotVisualizer
    {
        private string _outputDirectory;

        public ScottPlotVisualizer(string outputDir = "graphs")
        {
            _outputDirectory = outputDir;
            Directory.CreateDirectory(_outputDirectory);
        }

        public void SaveIndividualGraphs(List<string> selectedOutputVars, VisualizationData data)
        {
            int numGraphs = data.SeriesCount;

            for (int i = 0; i < numGraphs; i++)
            {
                double[] timeSeries = data.TimeHistory.ToArray();
                double[] ySeries = data.GetYSeries(i);

                var plt = new Plot();

                string title = "";
                var outputVar = selectedOutputVars[i];
                var parts = outputVar.Split('_');
                if (parts[0] == "U")
                {
                    title += "Напряжение на ";
                }
                else
                {
                    title += "Сила тока на ";
                }
                title += $"{parts[1]}";

                plt.Title($"{title} во времени");
                plt.XLabel("Время, t");
                plt.YLabel($"{title}");

                plt.Add.Scatter(timeSeries, ySeries);

                string filePath = Path.Combine(_outputDirectory, $"{title} во времени.png");
                plt.SavePng(filePath, 800, 600);
            }
        }
    }
}