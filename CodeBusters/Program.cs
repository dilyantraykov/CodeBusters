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

        public static int DistanceFromBaseToGetToBorder = MaxGhostBustDistance*2;

        public static int OptimalDistanceFromBaseToCatchGhosts =
            Geometry.GetHypothenuseOfRightTriangle(MaxGhostBustDistance);

        public const int MaxHelpDistance = 5000;

        public const int MaxDistanceFromBaseToReleaseGhost = 1600;

        public static Point Team0Base = new Point(0, 0);
        public static Point Team1Base = new Point(MaxWidth, MaxHeight);

        public static Point TopRightCorner = new Point(MaxWidth, 0);
        public static Point BottomLeftCorner = new Point(0, MaxHeight);
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
            if (turn < 50)
            {
                return GameState.Early;
            }
            else if (turn < 100)
            {
                return GameState.MidEarly;
            }
            else if (turn < 180)
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
            InitializeMovingPoint();
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
            Console.Error.WriteLine("Processing turn {0}...", gameState);

            this.StunRecovery -= 1;
            var finished = false;

            if (gameState == GameState.Late)
            {
                myBusters.Values.ToList().ForEach(b => b.IsInterseptor = true);
                Console.Error.WriteLine("----- Transforming interseptors...");
            }

            if (this.CanReleaseGhost())
            {
                this.Release();
                return;
            }

            finished = ProcessEnemies(ref enemyBusters, ghosts);

            if (!finished && this.ShouldGoBackToBase())
            {
                Console.Error.WriteLine("Should go back to base...");

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

        internal bool ShouldGoStraightToBase(int teamCoeff, int targetX, int targetY, GameState gameState)
        {
            Console.Error.WriteLine("Going straight to base...");

            if (teamCoeff == 1)
            {
                var isOnTopBorder = this.Point.X <= targetX && this.Point.Y == 0;
                var isOnLeftBorder = this.Point.X == 0 && this.Point.Y <= targetY;
                var isTooEarlyToRunAway = gameState == GameState.Early;

                return isOnTopBorder || isOnLeftBorder || isTooEarlyToRunAway;
            }
            else
            {
                var isOnBottomBorder = this.Point.X >= targetX && this.Point.Y == Constants.MaxHeight;
                var isOnRightBorder = this.Point.X == Constants.MaxWidth && this.Point.Y >= targetY;
                var isTooEarlyToRunAway = gameState == GameState.Early;

                return isOnBottomBorder || isOnRightBorder || isTooEarlyToRunAway;
            }
        }

        internal void MoveTowardsBase(GameState gameState)
        {
            Console.Error.WriteLine("Moving towards base...");

            if (gameState == GameState.Late)
            {
                this.Move(Constants.TopRightCorner);
                return;
            }

            var targetX = this.BasePoint.X + (this.TeamCoeff * Constants.MaxGhostBustDistance * 2);
            var targetY = this.BasePoint.Y + (this.TeamCoeff * Constants.MaxGhostBustDistance * 2);

            if (ShouldGoStraightToBase(this.TeamCoeff, targetX, targetY, gameState))
            {
                this.Move(this.BasePoint);
                return;
            }

            this.MoveToBaseMidPoints(this.BasePoint, targetX, targetY);
        }
        
        private void MoveToBaseMidPoints(Point basePoint, int targetX, int targetY)
        {
            if (this.TeamId == 0)
            {
                if (this.Point.X > targetX)
                {
                    this.Move(new Point(targetX, basePoint.Y));
                }
                else if (this.Point.X < targetX)
                {
                    this.Move(new Point(basePoint.X, targetY));
                }
            }
            else
            {
                if (this.Point.X < targetX)
                {
                    this.Move(new Point(targetX, basePoint.Y));
                }
                else if (this.Point.X > targetX)
                {
                    this.Move(new Point(basePoint.X, targetY));
                }
            }
        }

        internal void InitializeMovingPoint()
        {
            Console.Error.WriteLine("Initializing moving point...");

            if (this.Position == 1)
            {
                this.MovingPoint = this.BasePoint;
            }
            else if (this.Position % 2 == 0)
            {
                this.MovingPoint = this.TeamId == 0 ? Constants.TopRightCorner : Constants.BottomLeftCorner;
            }
            else
            {
                this.MovingPoint = this.TeamId == 0 ? Constants.BottomLeftCorner : Constants.TopRightCorner;
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
                    this.HelpPoint = null;
                    return true;
                }
                else if (this.IsTooCloseToGhost(ghost))
                {
                    Console.Error.WriteLine("Is too close to ghost...");

                    this.Move(this.BasePoint);
                    return true;
                }
                else if (this.HelpPoint == null && 
                    this.IsInRange(ghost.Point, Constants.MaxHelpDistance) &&
                    !this.IsInterseptor && 
                    gameState != GameState.Late)
                {
                    Console.Error.WriteLine("Setting help point: {0}", ghost.Point);

                    this.HelpPoint = ghost.Point;
                }
            }

            Console.Error.WriteLine("Processed ghosts...");
            return false;
        }

        internal bool ProcessEnemies(ref List<Buster> enemyBusters, List<Ghost> ghosts)
        {
            Console.Error.WriteLine("Processing enemies...");

            foreach (var buster in enemyBusters)
            {
                if (this.CanStunEnemyBuster(buster, ghosts))
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
                var baseOffset = Constants.MaxGhostBustDistance;
                var pointOffset = 400;

                var point1 = movingPoint
                            .AddX(this.TeamCoeff * -baseOffset + pointOffset)
                            .AddY(this.TeamCoeff * -baseOffset - pointOffset);

                var point2 = movingPoint
                        .AddX(this.TeamCoeff * -baseOffset - pointOffset)
                        .AddY(this.TeamCoeff * -baseOffset + pointOffset);

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
            var optimalDistance = Constants.MaxGhostBustDistance;
            var randomPoints = new List<Point>()
            {
                Constants.Team0Base.AddX(optimalDistance).AddY(optimalDistance),
                Constants.Team1Base.AddX(-optimalDistance).AddY(-optimalDistance),
                Constants.TopRightCorner.AddX(-optimalDistance).AddY(optimalDistance),
                Constants.BottomLeftCorner.AddX(optimalDistance).AddY(-optimalDistance)
            };

            var randomPoint = randomPoints[Constants.Rand.Next(0, randomPoints.Count)];

            return randomPoint;
        }

        internal Ghost GetGhostById(int id, List<Ghost> ghosts)
        {
            return ghosts.FirstOrDefault(g => g.Id == id);
        }

        #region Commands
        internal void Move(Point point)
        {
            Console.WriteLine("MOVE {0} {1} {2}", point.X, point.Y, this.IsInterseptor); // MOVE x y | BUST id | RELEASE
        }

        internal void Bust(int id)
        {
            Console.WriteLine("BUST {0} BUST {0} {1}", id, this.IsInterseptor); // MOVE x y | BUST id | RELEASE
        }

        internal void Release()
        {
            Console.WriteLine("RELEASE RELEASE {0}", this.IsInterseptor); // MOVE x y | BUST id | RELEASE
        }

        internal void Stun(int id)
        {
            this.StunRecovery = 20;
            Console.WriteLine("STUN {0} STUN {0} {1}", id, this.IsInterseptor); // MOVE x y | BUST id | RELEASE
        }
        #endregion

        #region Boolean Helpers

        internal bool HasReachedACorner()
        {
            return HasReachedBottomLeftCorner() || HasReachedBottomRightCorner()
                || HasReachedTopLeftCorner() || HasReachedTopRightCorner();
        }

        internal bool HasReachedBottomRightCorner()
        {
            return this.IsInRange(Constants.Team1Base, Constants.OptimalDistanceFromBaseToCatchGhosts);
        }

        internal bool HasReachedTopRightCorner()
        {
            return this.IsInRange(Constants.TopRightCorner, Constants.OptimalDistanceFromBaseToCatchGhosts);
        }

        internal bool HasReachedBottomLeftCorner()
        {
            return this.IsInRange(Constants.BottomLeftCorner, Constants.OptimalDistanceFromBaseToCatchGhosts);
        }

        internal bool HasReachedTopLeftCorner()
        {
            return this.IsInRange(Constants.Team0Base, Constants.OptimalDistanceFromBaseToCatchGhosts);
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

        internal bool CanStunEnemyBuster(Buster buster, List<Ghost> ghosts)
        {
            return (buster.State == State.CarryingGhost ||
                (buster.State == State.TrappingGhost) && this.GetGhostById(buster.GhostId, ghosts)?.Stamina < 10) &&
                this.StunRecovery <= 0 &&
                Geometry.CalculateDistance(this.Point, buster.Point) <= Constants.MaxGhostBustDistance;
        }

        internal bool CanInterseptEnemyBuster(Buster buster)
        {
            return (buster.State == State.CarryingGhost) &&
                this.StunRecovery <= 1 &&
                Geometry.CalculateDistance(this.Point, buster.Point) <= (Constants.VisibleDistance - 150);
        }
        #endregion

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

        public static Point GetPointAlongLine(Point p2, Point p1, double distance)
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