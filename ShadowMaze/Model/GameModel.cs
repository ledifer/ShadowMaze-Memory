using System;
using System.Collections.Generic;
using System.Timers;

namespace ShadowMaze.Model
{
    public class GameModel
    {
        public Maze Maze { get; private set; }
        public Player Player { get; private set; }
        public MemorySystem Memory { get; private set; }
        public List<Enemy> Enemies { get; private set; }

        public bool FullVisibility { get; set; } = false;

        public event Action? PlayerMoved;
        public event Action? MemoryChanged;
        public event Action? GameWon;
        public event Action? GameLost;
        public event Action? EnemiesUpdated;

        private System.Timers.Timer enemyTimer;
        private Random random = new Random();
        public bool IsFinished { get; private set; } = false;
        public void FinishGame()
        {
            IsFinished = true;
            StopEnemyTimer();
        }

        public GameModel(int mazeWidth, int mazeHeight, int memoryCapacity)
        {
            Maze = new Maze(mazeWidth, mazeHeight);
            Player = new Player(1, 1, memoryCapacity);
            Memory = new MemorySystem(memoryCapacity);
            // запоминаем область побольше, чтобы враг мог появиться далеко
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    Memory.Add(Player.X + dx, Player.Y + dy);

            Enemies = new List<Enemy>();
            SpawnInitialEnemies();

            enemyTimer = new System.Timers.Timer(300); // 300 мс
            enemyTimer.Elapsed += (s, e) => UpdateEnemies();
            enemyTimer.AutoReset = true;
            enemyTimer.Start();
        }

        public void StopEnemyTimer()
        {
            enemyTimer?.Stop();
            enemyTimer?.Dispose();
        }

        private void SpawnInitialEnemies()
        {
            var suitableCells = new List<(int x, int y)>();
            int minDist = 4;

            for (int x = 1; x < Maze.Width - 1; x++)
                for (int y = 1; y < Maze.Height - 1; y++)
                    if (!Maze.GetCell(x, y).IsWall && Memory.IsRemembered(x, y))
                    {
                        int dist = Math.Abs(x - Player.X) + Math.Abs(y - Player.Y);
                        if (dist >= minDist)
                            suitableCells.Add((x, y));
                    }

            // если нет подходящих клеток, то ставим принудительную дальнюю точку, если она проходима
            if (suitableCells.Count == 0)
            {
                (int x, int y) fallback = (Player.X + minDist, Player.Y);
                if (fallback.x >= Maze.Width) fallback.x = Maze.Width - 2;
                if (!Maze.GetCell(fallback.x, fallback.y).IsWall)
                    suitableCells.Add(fallback);
                else
                {
                    fallback = (Player.X, Player.Y + minDist);
                    if (fallback.y >= Maze.Height) fallback.y = Maze.Height - 2;
                    if (!Maze.GetCell(fallback.x, fallback.y).IsWall)
                        suitableCells.Add(fallback);
                }
            }

            var chosen = suitableCells[random.Next(suitableCells.Count)];
            Enemies.Add(new Enemy(Maze, chosen.x, chosen.y));
        }

        private void UpdateEnemies()
        {
            if (IsFinished) return;
            foreach (var enemy in Enemies)
                enemy.Update(Player, Memory);

            foreach (var enemy in Enemies)
            {
                if (enemy.X == Player.X && enemy.Y == Player.Y)
                {
                    FinishGame();
                    GameLost?.Invoke();
                    return;
                }
            }
            EnemiesUpdated?.Invoke();
        }

        public void MovePlayer(Direction direction)
        {
            if (IsFinished) return;
            int newX = Player.X;
            int newY = Player.Y;

            switch (direction)
            {
                case Direction.Up: newY--; break;
                case Direction.Down: newY++; break;
                case Direction.Left: newX--; break;
                case Direction.Right: newX++; break;
                default: return;
            }

            Cell? targetCell = Maze.GetCell(newX, newY);
            if (targetCell == null || targetCell.IsWall) return;

            Player.X = newX;
            Player.Y = newY;
            Memory.Add(Player.X, Player.Y);

            PlayerMoved?.Invoke();
            MemoryChanged?.Invoke();

            if (targetCell.IsExit)
            {
                FinishGame();
                GameWon?.Invoke();
            }
        }

        public bool IsInSight(int x, int y)
        {
            int dx = x - Player.X;
            int dy = y - Player.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= Player.VisionRadius;
        }
    }
}