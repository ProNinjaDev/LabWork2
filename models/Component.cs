namespace LabWork;

public class Component
{
    public string Name { get; set; }
    public string Type { get; set; } // "R", "C", "L", "E", "J"
    public double Value { get; set; } // сопротивление, емкость, индуктивность, напряжение, ток
    public int Node1 { get; set; } // первый узел, к которому подключен компонент
    public int Node2 { get; set; } // второй узел, к которому подключен компонент

    public int? ControlNode1 { get; set; }
    public int? ControlNode2 { get; set; }
    
    public override string ToString()
    {
        return $"Component {Name} ({Type}) connecting nodes {Node1} and {Node2}";
    }
}
