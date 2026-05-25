using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowMaze.Model
{
    public class Maze
    {
        public int Width { get; }
        public int Height { get; }
        private Cell[,] grid;
        private Random random = new Random();

        public Maze(int width, int height)
        {
            Width = width % 2 == 0 ? width + 1 : width;
            Height = height % 2 == 0 ? height + 1 : height;
            grid = new Cell[Width, Height];

            int exitX = Width - 2;
            int exitY = Height - 2;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    grid[x, y] = new Cell(false);

            for (int x = 0; x < Width; x++)
            {
                grid[x, 0].IsWall = true;
                grid[x, Height - 1].IsWall = true;
            }
            for (int y = 0; y < Height; y++)
            {
                grid[0, y].IsWall = true;
                grid[Width - 1, y].IsWall = true;
            }

            // рекурсивное деление
            Divide(1, 1, Width - 2, Height - 2);

            // соединяем все изолированные области
            ConnectAllComponents();
            int extraPassages = (Width * Height) / 20;
            AddExtraPassages(extraPassages);

            RemoveDanglingPassages();
            RemoveIsolatedWalls();

            // устанавливаем выход
            SetExit(exitX, exitY);

            // финальная проверка связности 
            if (!HasPath(1, 1, exitX, exitY))
            {
                ForcePath(1, 1, exitX, exitY);
            }
        }

        public Cell? GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return grid[x, y];
        }

        public void SetCell(int x, int y, Cell cell)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                grid[x, y] = cell;
        }

        private void SetExit(int x, int y)
        {
            Cell? cell = GetCell(x, y);
            if (cell != null)
            {
                cell.IsExit = true;
                cell.IsWall = false;
            }
        }

        private void Divide(int x1, int y1, int x2, int y2)
        {
            int width = x2 - x1 + 1;
            int height = y2 - y1 + 1;

            if (width < 3 || height < 3) return;

            bool vertical = width > height;

            int wallX = x1 + (vertical ? random.Next(1, width / 2) * 2 : 0);
            int wallY = y1 + (!vertical ? random.Next(1, height / 2) * 2 : 0);

            if (vertical)
            {
                for (int y = y1; y <= y2; y++)
                    grid[wallX, y].IsWall = true;

                int passageY = random.Next(y1, y2 + 1);
                grid[wallX, passageY].IsWall = false;
            }
            else
            {
                for (int x = x1; x <= x2; x++)
                    grid[x, wallY].IsWall = true;

                int passageX = random.Next(x1, x2 + 1);
                grid[passageX, wallY].IsWall = false;
            }

            if (vertical)
            {
                Divide(x1, y1, wallX - 1, y2);
                Divide(wallX + 1, y1, x2, y2);
            }
            else
            {
                Divide(x1, y1, x2, wallY - 1);
                Divide(x1, wallY + 1, x2, y2);
            }
        }

        private void ConnectAllComponents()
        {
            bool[,] visited = new bool[Width, Height];
            List<List<(int, int)>> components = new List<List<(int, int)>>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!grid[x, y].IsWall && !visited[x, y])
                    {
                        List<(int, int)> component = new List<(int, int)>();
                        Queue<(int, int)> queue = new Queue<(int, int)>();
                        queue.Enqueue((x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            var (cx, cy) = queue.Dequeue();
                            component.Add((cx, cy));

                            foreach (var (dx, dy) in new (int, int)[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                            {
                                int nx = cx + dx, ny = cy + dy;
                                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !visited[nx, ny] && !grid[nx, ny].IsWall)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }
                        components.Add(component);
                    }
                }
            }

            if (components.Count <= 1) return;

            List<(int, int)> mainComponent = components.FirstOrDefault(c => c.Contains((1, 1))) ?? components[0];
            HashSet<(int, int)> mainSet = new HashSet<(int, int)>(mainComponent);

            foreach (var comp in components)
            {
                if (comp == mainComponent) continue;

                bool connected = false;
                foreach (var (x, y) in comp)
                {
                    foreach (var (dx, dy) in new (int, int)[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && grid[nx, ny].IsWall)
                        {
                            int nnx = nx + dx, nny = ny + dy;
                            if (nnx >= 0 && nnx < Width && nny >= 0 && nny < Height && mainSet.Contains((nnx, nny)))
                            {
                                grid[nx, ny].IsWall = false;
                                mainSet.Add((nx, ny));
                                connected = true;
                                break;
                            }
                        }
                    }
                    if (connected) break;
                }

                if (!connected)
                {
                    foreach (var (x, y) in comp)
                    {
                        foreach (var (dx, dy) in new (int, int)[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && grid[nx, ny].IsWall)
                            {
                                int nnx = nx + dx, nny = ny + dy;
                                if (nnx >= 0 && nnx < Width && nny >= 0 && nny < Height && !grid[nnx, nny].IsWall)
                                {
                                    grid[nx, ny].IsWall = false;
                                    mainSet.Add((nx, ny));
                                    mainSet.UnionWith(comp);
                                    connected = true;
                                    break;
                                }
                            }
                        }
                        if (connected) break;
                    }
                }
            }
        }

        private void RemoveDanglingPassages()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (!grid[x, y].IsWall)
                        {
                            int neighbors = 0;
                            foreach (var (dx, dy) in new (int, int)[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !grid[nx, ny].IsWall)
                                    neighbors++;
                            }
                            if (neighbors == 0)
                            {
                                grid[x, y].IsWall = true;
                                changed = true;
                            }
                        }
                    }
                }
            }
        }

        private void RemoveIsolatedWalls()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (grid[x, y].IsWall)
                        {
                            int wallNeighbors = 0;
                            foreach (var (dx, dy) in new (int, int)[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && grid[nx, ny].IsWall)
                                    wallNeighbors++;
                            }
                            if (wallNeighbors == 0 && x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
                            {
                                grid[x, y].IsWall = false;
                                changed = true;
                            }
                        }
                    }
                }
            }
        }

        public bool HasPath(int startX, int startY, int endX, int endY)
        {
            bool[,] visited = new bool[Width, Height];
            Queue<(int, int)> queue = new Queue<(int, int)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x == endX && y == endY) return true;

                foreach (var (dx, dy) in new (int, int)[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !visited[nx, ny])
                    {
                        Cell? cell = GetCell(nx, ny);
                        if (cell != null && !cell.IsWall)
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }
            return false;
        }

        private void ForcePath(int startX, int startY, int endX, int endY)
        {
            int x = startX, y = startY;
            while (x != endX || y != endY)
            {
                grid[x, y].IsWall = false;
                if (x < endX) x++;
                else if (x > endX) x--;
                if (y < endY) y++;
                else if (y > endY) y--;
            }
            grid[endX, endY].IsWall = false;
        }

        private void AddExtraPassages(int count)
        {
            List<(int, int)> wallCandidates = new List<(int, int)>();

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    if (grid[x, y].IsWall)
                    {
                        // проверка, что стена разделяет два прохода (вертикально или горизонтально)
                        bool hasPassageVertical = (GetCell(x - 1, y) != null && !GetCell(x - 1, y).IsWall) &&
                                                  (GetCell(x + 1, y) != null && !GetCell(x + 1, y).IsWall);
                        bool hasPassageHorizontal = (GetCell(x, y - 1) != null && !GetCell(x, y - 1).IsWall) &&
                                                    (GetCell(x, y + 1) != null && !GetCell(x, y + 1).IsWall);

                        if (hasPassageVertical || hasPassageHorizontal)
                            wallCandidates.Add((x, y));
                    }
                }
            }

            // случайный выбор стены и превращение в проход
            for (int i = 0; i < count && wallCandidates.Count > 0; i++)
            {
                int index = random.Next(wallCandidates.Count);
                var (x, y) = wallCandidates[index];
                grid[x, y].IsWall = false;
                wallCandidates.RemoveAt(index);
            }
        }
    }
}