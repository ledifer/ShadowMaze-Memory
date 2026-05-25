using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowMaze.Model
{
    public class Enemy
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsSlowed { get; set; } = false;
        public bool IsPatrol { get; set; } = false; 
        private Maze maze;
        private Random random = new Random();
        private int moveCooldown = 5; 


        public Enemy(Maze maze, int startX, int startY)
        {
            this.maze = maze;
            X = startX;
            Y = startY;
        }

        public void Update(Player player, MemorySystem memory, List<Enemy> allEnemies)
        {

            if (moveCooldown > 0)
            {
                moveCooldown--;
                return;
            }
            if (IsSlowed)
                moveCooldown = 5;            // замедленные враги
            else if (!IsPatrol)
                moveCooldown = 1;            // преследователь быстрый (1 такт)
            else
                moveCooldown = 2;            // патрульные обычные (2 такта)

            if (IsPatrol)
            {
                var allNeighbors = GetValidNeighborsIgnoreMemory();
                allNeighbors.RemoveAll(cell => allEnemies.Any(e => e != this && e.X == cell.Item1 && e.Y == cell.Item2));
                if (allNeighbors.Count > 0)
                {
                    var (nx, ny) = allNeighbors[random.Next(allNeighbors.Count)];
                    X = nx;
                    Y = ny;
                }
                return;
            }

            if (!memory.IsRemembered(X, Y))
            {
                MoveTowardNearestMemory(memory);
                return;
            }

            var path = FindPath(X, Y, player.X, player.Y, memory);
            if (path != null && path.Count > 1)
            {
                var (nextX, nextY) = path[1];
                if (allEnemies.Any(e => e != this && e.X == nextX && e.Y == nextY))
                    return;
                X = nextX;
                Y = nextY;
            }
            else
            {
                var neighbors = GetValidMemoryNeighbors(memory);
                neighbors.RemoveAll(cell => allEnemies.Any(e => e != this && e.X == cell.Item1 && e.Y == cell.Item2));
                if (neighbors.Count > 0)
                {
                    var (nx, ny) = neighbors[random.Next(neighbors.Count)];
                    X = nx;
                    Y = ny;
                }
            }
        }

        private void MoveTowardNearestMemory(MemorySystem memory)
        {
            var target = FindNearestRememberedCell(memory);
            if (target == null) return;

            var path = FindPath(X, Y, target.Value.Item1, target.Value.Item2, memory, ignoreMemory: true);
            if (path != null && path.Count > 1)
            {
                var (nextX, nextY) = path[1];
                X = nextX;
                Y = nextY;
            }
        }

        private (int, int)? FindNearestRememberedCell(MemorySystem memory)
        {
            var visited = new bool[maze.Width, maze.Height];
            var queue = new Queue<(int, int)>();
            queue.Enqueue((X, Y));
            visited[X, Y] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (memory.IsRemembered(x, y) && !maze.GetCell(x, y)!.IsWall)
                    return (x, y);

                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i], ny = y + dy[i];
                    if (nx >= 0 && nx < maze.Width && ny >= 0 && ny < maze.Height &&
                        !visited[nx, ny] && !maze.GetCell(nx, ny)!.IsWall)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
            return null;
        }

        private List<(int, int)>? FindPath(int startX, int startY, int goalX, int goalY,
            MemorySystem memory, bool ignoreMemory = false)
        {
            var openSet = new SortedSet<(int f, int x, int y)>();
            var cameFrom = new Dictionary<(int, int), (int, int)>();
            var gScore = new Dictionary<(int, int), int>();
            int Heuristic(int x, int y) => Math.Abs(x - goalX) + Math.Abs(y - goalY);

            gScore[(startX, startY)] = 0;
            openSet.Add((Heuristic(startX, startY), startX, startY));

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);
                int x = current.x, y = current.y;
                if (x == goalX && y == goalY)
                    return ReconstructPath(cameFrom, (x, y));

                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i], ny = y + dy[i];
                    if (nx >= 0 && nx < maze.Width && ny >= 0 && ny < maze.Height &&
                        !maze.GetCell(nx, ny)!.IsWall)
                    {
                        if (!ignoreMemory && !memory.IsRemembered(nx, ny)) continue;
                        int tentativeG = gScore[(x, y)] + 1;
                        if (!gScore.ContainsKey((nx, ny)) || tentativeG < gScore[(nx, ny)])
                        {
                            gScore[(nx, ny)] = tentativeG;
                            int f = tentativeG + Heuristic(nx, ny);
                            openSet.Add((f, nx, ny));
                            cameFrom[(nx, ny)] = (x, y);
                        }
                    }
                }
            }
            return null;
        }

        private List<(int, int)> ReconstructPath(Dictionary<(int, int), (int, int)> cameFrom, (int, int) current)
        {
            var path = new List<(int, int)>();
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(current);
            path.Reverse();
            return path;
        }

        private List<(int, int)> GetValidMemoryNeighbors(MemorySystem memory)
        {
            var neighbors = new List<(int, int)>();
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                int nx = X + dx[i], ny = Y + dy[i];
                if (nx >= 0 && nx < maze.Width && ny >= 0 && ny < maze.Height &&
                    !maze.GetCell(nx, ny)!.IsWall &&
                    memory.IsRemembered(nx, ny))
                {
                    neighbors.Add((nx, ny));
                }
            }
            return neighbors;
        }

        private List<(int, int)> GetValidNeighborsIgnoreMemory()
        {
            var neighbors = new List<(int, int)>();
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                int nx = X + dx[i], ny = Y + dy[i];
                if (nx >= 0 && nx < maze.Width && ny >= 0 && ny < maze.Height &&
                    !maze.GetCell(nx, ny)!.IsWall)
                {
                    neighbors.Add((nx, ny));
                }
            }
            return neighbors;
        }
    }
}