namespace LabWork;

public class StateSpaceModel
{
    public double[,] A { get; set; }
    public double[,] B { get; set; }
    public double[,] C { get; set; }
    public double[,] D { get; set; }
    public List<string> StateVariables { get; set; } = new List<string>();
    public List<string> InputVariables { get; set; } = new List<string>();
    public List<string> OutputVariables { get; set; } = new List<string>();
}