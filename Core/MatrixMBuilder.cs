namespace LabWork;

public class MatrixMBuilder
{
    public (List<List<double>> mMatrix, List<string> rowLabels, List<string> columnLabels) BuildMatrix(List<Component> treeBranches, List<Component> chords)
    {
        // столбцы - ветви дерева, строки - хорды
        var columnLabels = treeBranches.Select(b => b.Name).ToList();
        var rowLabels = chords.Select(c => c.Name).ToList();

        var mMatrix = new List<List<double>>();
        for (int i = 0; i < rowLabels.Count; i++)
        {
            // кол-во хорд x кол-во ветвей дерева
            mMatrix.Add(new List<double>(new double[columnLabels.Count]));
        }

        // дерево в виде списка смежности
        var treeAdjacencyList = new Dictionary<int, List<(int neighbor, Component branch)>>();
        foreach (var branch in treeBranches)
        {
            if (!treeAdjacencyList.ContainsKey(branch.Node1)) treeAdjacencyList[branch.Node1] = new List<(int, Component)>();
            if (!treeAdjacencyList.ContainsKey(branch.Node2)) treeAdjacencyList[branch.Node2] = new List<(int, Component)>();
            
            // ребро в обе стороны
            treeAdjacencyList[branch.Node1].Add((branch.Node2, branch));
            treeAdjacencyList[branch.Node2].Add((branch.Node1, branch));
        }

        // проход по каждой хорде, чтобы заполнить соответствующую ей строку в матрице
        for (int i = 0; i < chords.Count; i++)
        {
            var chord = chords[i];
            
            var path = FindPathInTree(treeAdjacencyList, chord.Node1, chord.Node2);

            if (path == null) // а надо ли?
            {
                Console.WriteLine($"Couldn't find a path in the tree for the chord {chord.Name}");
                continue;
            }

            // направление обхода контура задается направлеинем хорды (от ноды 1 к ноде 2)
            int currentNode = chord.Node1; 
            foreach (var branch in path)
            {
                int colIndex = columnLabels.IndexOf(branch.Name);
                if (colIndex == -1) continue; // На всякий случай
                
                // направление прямое - 1
                // обратное - -1
                if (branch.Node1 == currentNode)
                {
                    mMatrix[i][colIndex] = 1.0;
                    currentNode = branch.Node2;
                }
                else
                {
                    mMatrix[i][colIndex] = -1.0;
                    currentNode = branch.Node1;
                }
            }
        }

        return (mMatrix, rowLabels, columnLabels);
    }

    // путь между двумя узлами (bfs)
    private List<Component> FindPathInTree(Dictionary<int, List<(int, Component)>> adjList, int startNode, int endNode)
    {
        var queue = new Queue<int>();
        queue.Enqueue(startNode);
        
        // ключ - нода, значение - откуда пришли и по какому брэнчу
        var cameFrom = new Dictionary<int, (int parent, Component branch)>();
        cameFrom[startNode] = (-1, null); // у стартовой ноды нет родителя 

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == endNode) break; // конечный узел

            foreach (var (neighbor, branch) in adjList[current])
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = (current, branch);
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        // восстанавливаем путь
        if (!cameFrom.ContainsKey(endNode)) return null;

        var path = new List<Component>();
        int currentNode = endNode;
        while (currentNode != startNode)
        {
            var (parent, branch) = cameFrom[currentNode];
            path.Add(branch);
            currentNode = parent;
        }

        path.Reverse(); // нужно startNode к endNode
        return path;
    }
}