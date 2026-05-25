using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ShadowMaze.Model
{
    public class GameModel
    {
        public Maze Maze { get; private set; }
        public Player Player { get; private set; }
        public MemorySystem Memory { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public List<Item> Items { get; private set; }
        public List<ActiveEffect> ActiveEffects { get; private set; }

        private bool chaserSpawned = false; 
        public bool FullVisibility { get; set; } = false;
        public bool IsFinished { get; private set; } = false;

        public event Action? PlayerMoved;
        public event Action? MemoryChanged;
        public event Action? GameWon;
        public event Action? GameLost;
        public event Action? EnemiesUpdated;
        public event Action? ItemsUpdated;
        public event Action? EffectsUpdated;

        private System.Timers.Timer enemyTimer;
        private Random random = new Random();

        private DateTime? startTime = null;
        private DateTime? endTime = null;
        public bool TimerStarted => startTime.HasValue;
        public string GetElapsedString()
        {
            if (!startTime.HasValue) return "00:00";
            TimeSpan elapsed = (endTime ?? DateTime.Now) - startTime.Value;
            return $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }

        private int initialMemoryCapacity;
        private int memoryBoostAmount = 0;
        private int initialVisionRadius;
        private int visionBoostAmount = 0;

        public GameModel(int mazeWidth, int mazeHeight, int memoryCapacity)
        {
            Maze = new Maze(mazeWidth, mazeHeight);
            Player = new Player(1, 1, memoryCapacity);
            Memory = new MemorySystem(memoryCapacity);

            initialMemoryCapacity = memoryCapacity;
            initialVisionRadius = Player.VisionRadius;

            Enemies = new List<Enemy>();
            Items = new List<Item>();
            ActiveEffects = new List<ActiveEffect>();

            SpawnInitialEnemies();
            SpawnItems();

            enemyTimer = new System.Timers.Timer(300);
            enemyTimer.Elapsed += (s, e) => UpdateEnemies();
            enemyTimer.AutoReset = true;
            enemyTimer.Start();
        }
        public void Pause()
        {
            enemyTimer?.Stop();
        }

        public void Resume()
        {
            if (!IsFinished)
                enemyTimer?.Start();
        }

        public void StopEnemyTimer()
        {
            enemyTimer?.Stop();
            enemyTimer?.Dispose();
        }

        public void FinishGame()
        {
            if (!IsFinished)
                endTime = DateTime.Now;
            IsFinished = true;
            StopEnemyTimer();
        }

        private void SpawnInitialEnemies()
        {
            // собираем все проходимые клетки, удалённые от старта (dist >= 5)
            var allPatrolCells = new List<(int x, int y)>();
            for (int x = 1; x < Maze.Width - 1; x++)
                for (int y = 1; y < Maze.Height - 1; y++)
                    if (!Maze.GetCell(x, y).IsWall)
                    {
                        int distToStart = Math.Abs(x - 1) + Math.Abs(y - 1);
                        if (distToStart >= 5)
                            allPatrolCells.Add((x, y));
                    }

            // выбираем до 4 патрульных, минимальное расстояние между ними >= 7
            List<(int x, int y)> chosenPatrols = new List<(int x, int y)>();
            var shuffled = allPatrolCells.OrderBy(_ => random.Next()).ToList();
            foreach (var cell in shuffled)
            {
                bool tooClose = false;
                foreach (var p in chosenPatrols)
                {
                    if (Math.Abs(cell.x - p.x) + Math.Abs(cell.y - p.y) < 7)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                {
                    chosenPatrols.Add(cell);
                    if (chosenPatrols.Count >= 4) break;
                }
            }

            while (chosenPatrols.Count < 4 && allPatrolCells.Count > chosenPatrols.Count)
            {
                var cell = allPatrolCells[random.Next(allPatrolCells.Count)];
                bool tooClose = false;
                foreach (var p in chosenPatrols)
                {
                    if (Math.Abs(cell.x - p.x) + Math.Abs(cell.y - p.y) < 3)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                    chosenPatrols.Add(cell);
            }

            // создаём патрульных
            foreach (var pos in chosenPatrols)
                Enemies.Add(new Enemy(Maze, pos.x, pos.y) { IsPatrol = true });
        }

        private void SpawnItems()
        {
            AddItemsOfType(ItemType.MemoryBoost, 2);
            AddItemsOfType(ItemType.VisionBoost, 2);
            AddItemsOfType(ItemType.SlowCrystal, 2);
        }

        private void AddItemsOfType(ItemType type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x, y;
                int attempts = 0;
                do
                {
                    x = random.Next(1, Maze.Width - 1);
                    y = random.Next(1, Maze.Height - 1);
                    attempts++;
                    if (attempts > 1000) break;
                } while (Maze.GetCell(x, y).IsWall ||
                         (x == 1 && y == 1) ||
                         (x == Maze.Width - 2 && y == Maze.Height - 2));

                Items.Add(new Item(x, y, type));
            }
        }

        private void UpdateEnemies()
        {
            if (IsFinished) return;

            UpdateEffects();

            foreach (var enemy in Enemies)
                enemy.Update(Player, Memory, Enemies);

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

        private void UpdateEffects()
        {
            if (ActiveEffects.Count == 0) return;

            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                ActiveEffect effect = ActiveEffects[i];
                effect.RemainingTicks--;
                if (effect.RemainingTicks <= 0)
                {
                    switch (effect.Type)
                    {
                        case ItemType.MemoryBoost:
                            memoryBoostAmount -= 20;
                            Memory.SetCapacity(initialMemoryCapacity + memoryBoostAmount);
                            break;
                        case ItemType.VisionBoost:
                            visionBoostAmount -= 1;
                            Player.VisionRadius = initialVisionRadius + visionBoostAmount;
                            break;
                        case ItemType.SlowCrystal:
                            if (!ActiveEffects.Any(e => e.Type == ItemType.SlowCrystal && e != effect))
                            {
                                foreach (var enemy in Enemies)
                                    enemy.IsSlowed = false;
                            }
                            break;
                    }
                    ActiveEffects.RemoveAt(i);
                }
            }
            EffectsUpdated?.Invoke();
        }

        public void MovePlayer(Direction direction)
        {
            if (IsFinished) return;
            if (!startTime.HasValue)
                startTime = DateTime.Now;

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
            if (!chaserSpawned && Memory.Count >= 7)
            {
                var chaser = new Enemy(Maze, 1, 1);
                Enemies.Add(chaser);
                chaserSpawned = true;
            }

            if (!chaserSpawned && Memory.Count >= 7)
            {
                var chaser = new Enemy(Maze, 1, 1);
                Enemies.Add(chaser);
                chaserSpawned = true;
            }

            // подбор предмета
            Item? picked = Items.FirstOrDefault(it => it.X == Player.X && it.Y == Player.Y);
            if (picked != null)
            {
                ApplyItemEffect(picked);
                Items.Remove(picked);
                ItemsUpdated?.Invoke();
            }

            if (targetCell.IsExit)
            {
                FinishGame();
                GameWon?.Invoke();
            }
        }

        private void ApplyItemEffect(Item item)
        {
            int duration = item.Type switch
            {
                ItemType.SlowCrystal => 10,
                ItemType.VisionBoost => 10,
                _ => 20
            };
            AddEffect(item.Type, duration);
        }

        private void AddEffect(ItemType type, int durationTicks)
        {
            ActiveEffect? existing = ActiveEffects.FirstOrDefault(e => e.Type == type);
            if (existing != null)
            {
                existing.RemainingTicks += durationTicks;
                EffectsUpdated?.Invoke();
                return;
            }

            ActiveEffects.Add(new ActiveEffect(type, durationTicks));
            switch (type)
            {
                case ItemType.MemoryBoost:
                    memoryBoostAmount += 20;
                    Memory.SetCapacity(initialMemoryCapacity + memoryBoostAmount);
                    break;
                case ItemType.VisionBoost:
                    visionBoostAmount += 1;
                    Player.VisionRadius = initialVisionRadius + visionBoostAmount;
                    break;
                case ItemType.SlowCrystal:
                    foreach (var enemy in Enemies)
                        enemy.IsSlowed = true;
                    break;
            }
            EffectsUpdated?.Invoke();
        }

        public bool IsInSight(int x, int y)
        {
            return Math.Abs(x - Player.X) <= Player.VisionRadius &&
                   Math.Abs(y - Player.Y) <= Player.VisionRadius;
        }
    }
}