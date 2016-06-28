using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CodeBusters
{
    public static class Constants
    {
        public static Random Rand = new Random();

        public const int MaxWidth = 16000;
        public const int MaxHeight = 9000;

        public const int VisibleDistance = 2200;

        public const int MaxBusterMoveDistance = 800;
        public const int MaxGhostMoveDistance = 400;

        public const int MinGhostBustDistance = 900;
        public const int MaxGhostBustDistance = 1760;

        public const int MaxDistanceFromBaseToReleaseGhost = 1600;

        public static Point Team0Base = new Point(0, 0);
        public static Point Team1Base = new Point(Constants.MaxWidth, Constants.MaxHeight);

        public static Point TopRightCorner = new Point(Constants.MaxWidth, 0);
        public static Point BottomLeftCorner = new Point(0, Constants.MaxHeight);
    }

    /**
     * Send your busters out into the fog to trap ghosts and bring them home!
     **/
    public class Player
    {
        static void Main(string[] args)
        {
            int bustersPerPlayer = int.Parse(Console.ReadLine()); // the amount of busters you control
            int ghostCount = int.Parse(Console.ReadLine()); // the amount of ghosts on the map
            int myTeamId = int.Parse(Console.ReadLine()); // if this is 0, your base is on the top left of the map, if it is one, on the bottom right

            var myBusters = new Dictionary<int, Buster>();
            int turn = 0;

            // game loop
            while (true)
            {
                turn++;
                int entitiesCount = int.Parse(Console.ReadLine()); // the number of busters and ghosts visible to you
                var ghosts = new List<Ghost>();
                var enemyBusters = new List<Buster>();

                // Process Visible Area
                for (int i = 0; i < entitiesCount; i++)
                {
                    string[] inputs = Console.ReadLine().Split(' ');
                    int entityId = int.Parse(inputs[0]); // buster id or ghost id
                    int x = int.Parse(inputs[1]);
                    int y = int.Parse(inputs[2]); // position of this buster / ghost
                    int entityType = int.Parse(inputs[3]); // the team id if it is a buster, -1 if it is a ghost.
                    int state = int.Parse(inputs[4]); // For busters: 0=idle, 1=carrying a ghost.
                    int value = int.Parse(inputs[5]); // For busters: Ghost id being carried. For ghosts: number of busters attempting to trap this ghost.

                    if (entityType == 0 || entityType == 1)
                    {
                        if (entityType == myTeamId)
                        {
                            if (!myBusters.ContainsKey(entityId))
                            {
                                myBusters.Add(entityId, new Buster(entityId, new Point(x, y), entityType, (State)state, value, myBusters.Count + 1, bustersPerPlayer));
                            }
                            else
                            {
                                myBusters[entityId].Point = new Point(x, y);
                                myBusters[entityId].State = (State)state;
                                if ((State)state == State.CarryingGhost || (State)state == State.TrappingGhost)
                                {
                                    myBusters[entityId].GhostId = value;
                                }
                                else
                                {
                                    myBusters[entityId].GhostId = -1;
                                }
                            }
                        }
                        else
                        {
                            enemyBusters.Add(new Buster(entityId, new Point(x, y), entityType, (State)state, value));
                        }
                    }
                    else if (entityType == -1)
                    {
                        var ghost = new Ghost(entityId, new Point(x, y), value, state);

                        ghosts.Add(ghost);
                    }
                }

                foreach (var buster in myBusters)
                {
                    var gameState = DefineGameState(turn);
                    buster.Value.ProcessTurn(myBusters, ref enemyBusters, ref ghosts, gameState);
                }
            }
        }

        private static GameState DefineGameState(int turn)
        {
            if (turn < 70)
            {
                return GameState.Early;
            }
            else if (turn < 120)
            {
                return GameState.MidEarly;
            }
            else if (turn < 170)
            {
                return GameState.MidLate;
            }
            else
            {
                return GameState.Late;
            }
        }
    }

    public class Entity
    {
        public Entity()
        {
        }

        public Entity(int id, Point point)
        {
            this.Id = id;
            this.Point = point;
        }

        public int Id { get; set; }
        public Point Point { get; set; }
    }

    public class Buster : Entity
    {
        private int teamId;

        public Buster()
        {
        }

        public Buster(int id, Point point, int teamId, State state, int ghostId)
            : base(id, point)
        {
            this.TeamId = teamId;
            this.TeamCoeff = teamId == 0 ? 1 : -1;
            this.State = state;
            this.GhostId = ghostId;

            this.StunRecovery = 0;
            this.HelpPoint = null;

            this.OppositeTeamBase = teamId == 0 ? Constants.Team1Base : Constants.Team0Base;
        }

        public Buster(int id, Point point, int teamId, State state, int ghostId, int position, int teamSize)
            : this(id, point, teamId, state, ghostId)
        {
            this.Position = position;
            this.IsInterseptor = this.Position == 2 || this.Position == 3;
            this.TeamSize = teamSize;
            InitializeMovingPoint(teamId);
        }

        public int TeamId
        {
            get { return this.teamId; }
            set
            {
                this.teamId = value;
                this.BasePoint = value == 0 ? Constants.Team0Base : Constants.Team1Base;
            }
        }

        public State State { get; set; }
        public int GhostId { get; set; }
        public Point BasePoint { get; set; }
        public int StunRecovery { get; set; }
        public bool HasFinishedCourse { get; set; }
        public Point MovingPoint { get; set; }
        public Point HelpPoint { get; set; }
        public bool IsInterseptor { get; set; }
        public Point OppositeTeamBase { get; private set; }
        public int Position { get; set; }
        public int TeamSize { get; set; }
        public int TeamCoeff { get; set; }

        public void ProcessTurn(Dictionary<int, Buster> myBusters, ref List<Buster> enemyBusters, ref List<Ghost> ghosts, GameState gameState)
        {
            Console.Error.WriteLine("Processing turn...");

            this.StunRecovery -= 1;
            bool finished;

            if (this.CanReleaseGhost())
            {
                this.Release();
                return;
            }

            finished = ProcessEnemies(ref enemyBusters);

            if (!finished && this.ShouldGoBackToBase())
            {
                this.MoveTowardsBase(gameState);
                return;
            }

            if (!finished)
            {
                finished = ProcessGhosts(ref ghosts, gameState);
            }

            if (!finished)
            {
                MoveRandomlyIfNoGhostsAreNear(this.TeamId, gameState);
            }
        }

        internal void MoveTowardsBase(GameState gameState)
        {
            Console.Error.WriteLine("Moving towards base...");

            if (this.TeamId == 0)
            {
                var targetX = Constants.MaxGhostBustDistance * 2;
                var targetY = Constants.MaxGhostBustDistance * 2;

                var distanceFromBase = Geometry.GetHypothenuseOfRightTriangle(targetX);

                if (gameState == GameState.Late)
                {
                    this.Move(Constants.TopRightCorner);
                    return;
                }

                var isOnTopBorder = this.Point.X <= targetX && this.Point.Y == 0;
                var isOnLeftBorder = this.Point.X == 0 && this.Point.Y <= targetY;
                var isTooEarlyToRunAway = gameState == GameState.Early;

                if (isOnTopBorder || isOnLeftBorder || isTooEarlyToRunAway)
                {
                    this.Move(Geometry.GetPointAlongLine(this.Point, Constants.Team0Base, Constants.MaxDistanceFromBaseToReleaseGhost - 1));
                    return;
                }

                if (this.Point.X > targetX)
                {
                    this.Move(new Point(targetX, 0));
                }
                else if (this.Point.X < targetX)
                {
                    this.Move(new Point(0, targetY));
                }
            }
            else
            {
                var targetX = Constants.Team1Base.X - (Constants.MaxGhostBustDistance * 2);
                var targetY = Constants.Team1Base.Y - (Constants.MaxGhostBustDistance * 2);

                if (gameState == GameState.Late)
                {
                    this.Move(Constants.BottomLeftCorner);
                    return;
                }

                var isOnBottomBorder = this.Point.X >= targetX && this.Point.Y == Constants.MaxHeight;
                var isOnRightBorder = this.Point.X == Constants.MaxWidth && this.Point.Y >= targetY;
                var isTooEarlyToRunAway = gameState == GameState.Early;

                if (isOnBottomBorder || isOnRightBorder || isTooEarlyToRunAway)
                {
                    this.Move(Geometry.GetPointAlongLine(this.Point, Constants.Team1Base, Constants.MaxDistanceFromBaseToReleaseGhost - 1));
                    return;
                }

                if (this.Point.X < targetX)
                {
                    this.Move(new Point(targetX, Constants.Team1Base.Y));
                }
                else if (this.Point.X > targetX)
                {
                    this.Move(new Point(Constants.MaxWidth, targetY));
                }
            }
        }

        internal void InitializeMovingPoint(int teamId)
        {
            Console.Error.WriteLine("Initializing moving point...");
            if (this.Position == 3)
            {
                if (teamId == 0)
                {
                    this.MovingPoint = Constants.Team1Base;
                }
                else
                {
                    this.MovingPoint = Constants.Team0Base;
                }
            }
            else if (this.Position == 2)
            {
                if (teamId == 0)
                {
                    this.MovingPoint = Constants.TopRightCorner;
                }
                else
                {
                    this.MovingPoint = Constants.BottomLeftCorner;
                }
            }
            else
            {
                if (teamId == 0)
                {
                    this.MovingPoint = Constants.BottomLeftCorner;
                }
                else
                {
                    this.MovingPoint = Constants.TopRightCorner;
                }
            }

            Console.Error.WriteLine("Moving point: {0}", this.MovingPoint);
        }

        internal bool ProcessGhosts(ref List<Ghost> ghosts, GameState gameState)
        {
            Console.Error.WriteLine("Processing ghosts...");

            ghosts = ghosts.OrderBy(g => g.Stamina).ToList();

            foreach (var ghost in ghosts)
            {
                if (this.CanBustGhost(ghost))
                {
                    Console.Error.WriteLine("Busting ghost...");
                    this.Bust(ghost.Id);
                    //ghost.BustersCount += 1;
                    this.HelpPoint = null;
                    //ghosts.Remove(ghost);
                    return true;
                }
                else if (this.IsTooCloseToGhost(ghost))
                {
                    Console.Error.WriteLine("Is too close to ghost...");
                    this.Move(this.BasePoint);
                    return true;
                }
                else if (this.HelpPoint == null && !this.IsInterseptor && gameState != GameState.Late)
                {
                    Console.Error.WriteLine("Setting help point...");
                    this.HelpPoint = ghost.Point;
                }
                else if (gameState == GameState.Late)
                {
                    Console.Error.WriteLine("Setting help point (GameState.Late)...");
                    this.HelpPoint = this.BasePoint
                        .AddX(Constants.MinGhostBustDistance)
                        .AddY(Constants.MinGhostBustDistance);
                }
            }

            Console.Error.WriteLine("Processed ghosts...");
            return false;
        }

        internal bool ProcessEnemies(ref List<Buster> enemyBusters)
        {
            Console.Error.WriteLine("Processing enemies...");

            foreach (var buster in enemyBusters)
            {
                if (this.CanStunEnemyBuster(buster))
                {
                    this.Stun(buster.Id);
                    buster.State = State.Stunned;
                    return true;
                }
                else if (this.CanInterseptEnemyBuster(buster))
                {
                    this.Move(buster.Point);
                    return true;
                }
            }

            return false;
        }

        internal void MoveRandomlyIfNoGhostsAreNear(int teamId, GameState gameState)
        {
            Console.Error.WriteLine("Moving randomly...");

            Point movingPoint;

            if (this.IsInterseptor)
            {
                movingPoint = this.OppositeTeamBase;
                var pointOffset = 300;

                var point1 = movingPoint
                            .AddX(this.TeamCoeff * -Constants.MaxGhostBustDistance + pointOffset)
                            .AddY(this.TeamCoeff * -Constants.MaxGhostBustDistance - pointOffset);

                var point2 = movingPoint
                        .AddX(this.TeamCoeff * -Constants.MaxGhostBustDistance - pointOffset)
                        .AddY(this.TeamCoeff * -Constants.MaxGhostBustDistance + pointOffset);

                movingPoint = point1;

                if (Geometry.IsSamePoint(this.Point, movingPoint))
                {
                    movingPoint = point2;
                }
            }
            else if (this.HelpPoint != null &&
                Geometry.IsSamePoint(this.Point, this.HelpPoint))
            {
                this.HelpPoint = null;
                movingPoint = this.MovingPoint;
            }
            else if (this.HelpPoint != null)
            {
                movingPoint = this.HelpPoint;
            }
            else if (HasReachedACorner())
            {
                movingPoint = this.GetRandomPoint();
                this.MovingPoint = movingPoint;
            }
            else
            {
                movingPoint = this.MovingPoint;
            }

            Move(movingPoint);
        }

        internal Point GetRandomPoint()
        {
            var optimalDistance = Geometry.GetLegsOfRightTriangle(Constants.MaxGhostBustDistance);
            var randomPoints = new List<Point>()
            {
                Constants.Team0Base.AddX(optimalDistance - 1).AddY(optimalDistance - 1),
                Constants.Team1Base.AddX(-optimalDistance + 1).AddY(-optimalDistance + 1),
                Constants.TopRightCorner.AddX(-optimalDistance + 1).AddY(optimalDistance - 1),
                Constants.BottomLeftCorner.AddX(optimalDistance - 1).AddY(-optimalDistance + 1)
            };

            var randomPoint = randomPoints[Constants.Rand.Next(0, randomPoints.Count)];

            return randomPoint;
        }

        internal bool HasReachedACorner()
        {
            return HasReachedBottomLeftCorner() || HasReachedBottomRightCorner()
                || HasReachedTopLeftCorner() || HasReachedTopRightCorner();
        }

        internal bool HasReachedBottomRightCorner()
        {
            return this.IsInRange(Constants.Team1Base, Constants.MaxGhostBustDistance);
        }

        internal bool HasReachedTopRightCorner()
        {
            return this.IsInRange(Constants.TopRightCorner, Constants.MaxGhostBustDistance);
        }

        internal bool HasReachedBottomLeftCorner()
        {
            return this.IsInRange(Constants.BottomLeftCorner, Constants.MaxGhostBustDistance);
        }

        internal bool HasReachedTopLeftCorner()
        {
            return this.IsInRange(Constants.Team0Base, Constants.MaxGhostBustDistance);
        }

        private void MoveDiagonallyDown()
        {
            this.MoveInUnits(Constants.MaxWidth, Constants.MaxHeight);
        }

        private void MoveDiagonallyUp()
        {
            this.MoveInUnits(-Constants.MaxWidth, -Constants.MaxHeight);
        }

        private void MoveUp()
        {
            this.MoveInUnits(-800, 0);
        }

        private void MoveDown()
        {
            this.MoveInUnits(800, 100);
        }

        private void MoveRight()
        {
            this.MoveInUnits(100, 800);
        }

        private void MoveLeft()
        {
            this.MoveInUnits(0, -800);
        }

        private void MoveInUnits(int verticalOffset, int horizontalOffset)
        {
            var newX = this.Point.X + verticalOffset;
            var nexY = this.Point.Y + horizontalOffset;
            this.Move(new Point(newX, nexY));
        }

        internal void Move(Point point)
        {
            Console.WriteLine("MOVE {0} {1} {2}", point.X, point.Y, this.StunRecovery); // MOVE x y | BUST id | RELEASE
        }

        internal void Bust(int id)
        {
            Console.WriteLine("BUST {0} BUST {0} {1}", id, this.StunRecovery); // MOVE x y | BUST id | RELEASE
        }

        internal void Release()
        {
            Console.WriteLine("RELEASE RELEASE {0}", this.StunRecovery); // MOVE x y | BUST id | RELEASE
        }

        internal void Stun(int id)
        {
            this.StunRecovery = 20;
            Console.WriteLine("STUN {0} STUN {0} {1}", id, this.StunRecovery); // MOVE x y | BUST id | RELEASE
        }

        internal Buster GetBusterById(List<Buster> busters, int id)
        {
            return busters.Find(b => b.Id == id);
        }

        internal Ghost GetClosestGhost(IList<Ghost> ghosts)
        {
            return ghosts.First();
        }

        internal bool IsInRange(Point point, double distance)
        {
            return Geometry.CalculateDistance(this.Point, point) <= distance;
        }

        internal bool CanBustGhost(Ghost ghost)
        {
            return Geometry.CalculateDistance(this.Point, ghost.Point) >= Constants.MinGhostBustDistance &&
                Geometry.CalculateDistance(this.Point, ghost.Point) <= Constants.MaxGhostBustDistance;
        }

        internal bool CanReleaseGhost()
        {
            return this.GhostId != -1 && this.IsInRange(this.BasePoint, Constants.MaxDistanceFromBaseToReleaseGhost);
        }

        internal bool ShouldGoBackToBase()
        {
            return this.GhostId != -1 && this.State == State.CarryingGhost;
        }

        internal bool IsTooCloseToGhost(Ghost ghost)
        {
            return Geometry.CalculateDistance(this.Point, ghost.Point) < Constants.MinGhostBustDistance;
        }

        internal bool CanStunEnemyBuster(Buster buster)
        {
            return (buster.State == State.CarryingGhost ||
                (buster.State == State.TrappingGhost)) &&
                this.StunRecovery <= 0 &&
                Geometry.CalculateDistance(this.Point, buster.Point) <= Constants.MaxGhostBustDistance;
        }

        internal bool CanInterseptEnemyBuster(Buster buster)
        {
            return (buster.State == State.CarryingGhost) &&
                this.StunRecovery <= 1 &&
                Geometry.CalculateDistance(this.Point, buster.Point) <= (Constants.VisibleDistance - 100);
        }

        public override string ToString()
        {
            return string.Format("{0}) ({1},{2}) ({3} #{4})", this.Id, this.Point.X, this.Point.Y, this.State.ToString(), this.GhostId);
        }
    }

    public class Ghost : Entity
    {
        public Ghost()
        {
        }

        public Ghost(int id, Point point, int bustersCount, int stamina)
            : base(id, point)
        {
            this.BustersCount = bustersCount;
            this.Stamina = stamina;
        }

        public int BustersCount { get; set; }
        public int Stamina { get; set; }

        public override string ToString()
        {
            return string.Format("Ghost #{0} at ({1},{2}) ({3})", this.Id, this.Point.X, this.Point.Y, this.BustersCount);
        }
    }

    public class Point
    {
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public Point AddX(int offset)
        {
            return new Point(this.X + offset, this.Y);
        }

        public Point AddY(int offset)
        {
            return new Point(this.X, this.Y + offset);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", this.X, this.Y);
        }
    }

    public enum State
    {
        Idle = 0,
        CarryingGhost = 1,
        Stunned = 2,
        TrappingGhost = 3
    }

    public enum GameState
    {
        Early = 0,
        MidEarly = 1,
        MidLate = 2,
        Late = 3
    }

    public static class Geometry
    {
        public static int GetLegsOfRightTriangle(int hypothenuse)
        {
            var result = (int)Math.Sqrt((hypothenuse * hypothenuse) / 2);

            return result;
        }

        public static int GetHypothenuseOfRightTriangle(int legLenght)
        {
            var result = (int)Math.Sqrt((legLenght * legLenght) * 2);

            return result;
        }

        public static bool IsSamePoint(Point p1, Point p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        public static Point GetPointAlongLine(Point p2, Point p1, int distance)
        {
            Point vector = new Point(p2.X - p1.X, p2.Y - p1.Y);
            double c = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            double a = distance / c;

            var newX = (int)(p1.X + vector.X * a);
            var newY = (int)(p1.Y + vector.Y * a);

            return new Point(newX, newY);
        }
    }
}