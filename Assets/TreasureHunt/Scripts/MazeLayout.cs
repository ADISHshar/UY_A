using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunt
{
    // A spanning tree guarantees a route to every cell. The key is placed on the
    // start side of the final locked passage, so every generated level is solvable.
    public sealed class MazeLayout
    {
        public readonly int Width, Height;
        public readonly bool[,,] Open;
        public readonly List<Vector2Int> Route = new List<Vector2Int>();
        public readonly List<Vector2Int> Cells = new List<Vector2Int>();
        public static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        public MazeLayout(int width, int height, int seed)
        {
            Width = Mathf.Clamp(width, 5, 31); Height = Mathf.Clamp(height, 5, 31);
            Open = new bool[Width, Height, 4];
            var random = new System.Random(seed);
            var visited = new bool[Width, Height];
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var stack = new Stack<Vector2Int>();
            stack.Push(Vector2Int.zero); visited[0, 0] = true;
            var farthest = Vector2Int.zero; int longest = 0;
            while (stack.Count > 0)
            {
                var cell = stack.Peek();
                var choices = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    var n = cell + Directions[d];
                    if (n.x >= 0 && n.y >= 0 && n.x < Width && n.y < Height && !visited[n.x, n.y]) choices.Add(d);
                }
                if (choices.Count == 0) { stack.Pop(); continue; }
                int dir = choices[random.Next(choices.Count)];
                var next = cell + Directions[dir];
                Open[cell.x, cell.y, dir] = Open[next.x, next.y, (dir + 2) % 4] = true;
                visited[next.x, next.y] = true; parent[next] = cell; stack.Push(next);
                if (stack.Count > longest) { longest = stack.Count; farthest = next; }
            }
            for (var p = farthest; p != Vector2Int.zero; p = parent[p]) Route.Add(p);
            Route.Add(Vector2Int.zero); Route.Reverse();
            for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++) Cells.Add(new Vector2Int(x, y));
            for (int i = Cells.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var c = Cells[i]; Cells[i] = Cells[j]; Cells[j] = c; }
        }
        public Vector3 World(Vector2Int cell, float size) => new Vector3(cell.x * size, 0, cell.y * size);
    }
}
