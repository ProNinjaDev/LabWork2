
using System;
using System.Collections.Generic;
using System.Linq;

namespace LabWork;


public class TreeBuilder
{
    public (List<Component> treeBranches, List<Component> chords) BuildTree(List<Component> allComponents)
    {
        var componentPriorityOrder = new Dictionary<string, int>
        {
            { "E", 1 },
            { "C", 2 }, 
            { "R", 3 },
            // остальные не кандидаты в дереов
        };

        // отбираем только те компоненты, которые могут быть в дереве
        var candidatesForTree = allComponents
            .Where(c => componentPriorityOrder.ContainsKey(c.Type))
            .OrderBy(c => componentPriorityOrder[c.Type])
            .ToList();

        var treeBranches = new List<Component>();
        var connectedNodeGroups = new List<HashSet<int>>();

        // сначала каждый узел в схеме это отдельная группа
        // поиск все уникальных номеров узлов
        var allNodes = allComponents
            .SelectMany(c => new[] { c.Node1, c.Node2 })
            .Distinct()
            .ToList();

        foreach (var node in allNodes)
        {
            // создаем новую группу
            connectedNodeGroups.Add(new HashSet<int> { node });
        }

        // проход по кандидатам и добавление в дерево
        foreach (var component in candidatesForTree)
        {
            HashSet<int> group1 = null;
            HashSet<int> group2 = null;

            foreach (var group in connectedNodeGroups)
            {
                if (group.Contains(component.Node1))
                {
                    group1 = group;
                }
                if (group.Contains(component.Node2))
                {
                    group2 = group;
                }
            }

            // два узла в одной и той же группе не должны быть
            if (group1 != group2)
            {
                treeBranches.Add(component);

                group1.UnionWith(group2); // все узлы из group2 в group1
                connectedNodeGroups.Remove(group2);
            }
        }

        // список хорд (компоненты невошедшие в дерево)
        var treeBranchNames = new HashSet<string>(treeBranches.Select(b => b.Name));
        var chords = allComponents.Where(c => !treeBranchNames.Contains(c.Name)).ToList();

        // R, L, J
        var chordPriorityOrder = new Dictionary<string, int>
        {
            { "R", 1 },
            { "L", 2 },
            { "J", 3 },

            { "C", 4 }, // а надо ли?
            { "E", 5 } // а надо ли?
        };

        chords = chords.OrderBy(c => chordPriorityOrder.ContainsKey(c.Type) ? chordPriorityOrder[c.Type] : 99).ToList();

        return (treeBranches, chords);
    }
}

