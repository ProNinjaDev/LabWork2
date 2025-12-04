using LabWork.models;

namespace LabWork.Core
{
    public class EulerSolver
    {
        public void Solve(List<Component> components, StateSpaceModel stateSpaceModel, Dictionary<string, double> initialConditions, double step, double totalTime, VisualizationData dataContainer)
        {
            List<double> initialConditionsList = stateSpaceModel.StateVariables
                .Select(varName => initialConditions[varName])
                .ToList();
            List<double> inputValues = stateSpaceModel.InputVariables
                .Select(inputVar =>
                {
                    var parts = inputVar.Split('_');

                    string componentName = parts[1];
                    var component = components.FirstOrDefault(c => c.Name == componentName);
                    return component.Value;
                })
                .ToList();

            Vector X = new Vector(initialConditionsList);

            var A = new Matrix(stateSpaceModel.A);
            var B = new Matrix(stateSpaceModel.B);
            var C = new Matrix(stateSpaceModel.C);
            var D = new Matrix(stateSpaceModel.D);
            var V = new Vector(inputValues);

            for (double t = 0; t <= totalTime; t += step)
            {
                Vector Y_vector = C * X + D * V;

                dataContainer.AddDataPoint(t, Y_vector);

                Vector dX_dt = A * X + B * V;

                X = X + dX_dt * step;
            }
        }
    }
}