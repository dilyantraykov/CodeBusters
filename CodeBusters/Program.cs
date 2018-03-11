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

        public static int DistanceFromBaseToGetToBorder = MaxGhostBustDistance * 2;

        public static int OptimalDistanceFromCornerToCatchGhosts =
            Geometry.GetHypothenuseOfRightTriangle(MaxGhostBustDistance);

        public static int OptimalXandYFromCornerToCatchGhosts =
            Geometry.GetLegsOfRightTriangle(VisibleDistance);

        public const int MaxHelpDistance = 10000;

        public const int TurnsUntilStunnedBusterCanMoveAgain = 10;
        public const int TurnsUntilBusterCanStunAgain = 20;

        public const int MaxDistanceFromBaseToReleaseGhost = 1600;

        public static Point Team0Base = new Point(0, 0);
        public static Point Team1Base = new Point(MaxWidth, MaxHeight);

        public static Point TopRightCorner = new Point(MaxWidth, 0);
        public static Point BottomLeftCorner = new Point(0, MaxHeight);

        internal const int EarlyGameTurns = 50;
        internal const int MidEarlyGameTurns = 100;
        internal const int MidLateGameTurns = 150;
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
            var enemyBusters = new Dictionary<int, Buster>();
            var ghosts = new Dictionary<int, Ghost>();

            var gameContext = new GameContext()
            {
                BustersPerTeam = bustersPerPlayer,
                EnemyBusters = enemyBusters,
                EnemyScore = 0,
                Turn = 0,
                GhostCount = ghostCount,
                Ghosts = ghosts,
                MyBusters = myBusters,
                MyScore = 0
            };

            // game loop
            while (true)
            {
                gameContext.Turn++;
                int entitiesCount = int.Parse(Console.ReadLine()); // the number of busters and ghosts visible to you
                var visibleGhosts = new Dictionary<int, Ghost>();
                var visibleEnemies = new Dictionary<int, Buster>();

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
                                var buster = new Buster(entityId, new Point(x, y), entityType, (State)state, value, myBusters.Count + 1, bustersPerPlayer);
                                myBusters.Add(entityId, buster);
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
                            if (!enemyBusters.ContainsKey(entityId))
                            {
                                var buster = new Buster(entityId, new Point(x, y), entityType, (State)state, value);
                                enemyBusters.Add(entityId, buster);
                                visibleEnemies.Add(entityId, buster);
                            }
                            else
                            {
                                enemyBusters[entityId].Point = new Point(x, y);
                                enemyBusters[entityId].State = (State)state;
                                if ((State)state == State.CarryingGhost || (State)state == State.TrappingGhost)
                                {
                                    enemyBusters[entityId].GhostId = value;
                                }
                                else
                                {
                                    enemyBusters[entityId].GhostId = -1;
                                }

                                visibleEnemies.Add(entityId, enemyBusters[entityId]);
                            }
                        }
                    }
                    else if (entityType == -1)
                    {
                        if (!ghosts.ContainsKey(entityId))
                        {
                            var ghost = new Ghost(entityId, new Point(x, y), value, state);
                            ghosts.Add(entityId, ghost);
                            visibleGhosts.Add(entityId, ghost);
                        }
                        else
                        {
                            ghosts[entityId].Point = new Point(x, y);
                            ghosts[entityId].Stamina = state;
                            ghosts[entityId].BustersCount = value;
                            visibleGhosts.Add(entityId, ghosts[entityId]);
                        }
                    }
                }

                gameContext.MyBusters = myBusters;
                gameContext.VisibleEnemies = visibleEnemies;
                gameContext.VisibleGhosts = visibleGhosts;

                foreach (var buster in myBusters)
                {
                    buster.Value.ProcessTurn(gameContext);
                }
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
        private Point movingPoint;

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
            if (this.Position == 2)
            {
                this.InterseptorId = 1;
            }
            else if (this.Position == 4)
            {
                this.InterseptorId = 2;
            }
            else
            {
                this.InterseptorId = -1;
            }

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

        public Point MovingPoint
        {
            get { return this.movingPoint; }
            set
            {
                this.PreviousMovingPoint = this.movingPoint;

                this.movingPoint = value;
            }
        }

        public Point PreviousMovingPoint { get; set; }
        public Point HelpPoint { get; set; }
        public int InterseptorId { get; set; }
        public Point OppositeTeamBase { get; private set; }
        public int Position { get; set; }
        public int TeamSize { get; set; }
        public int TeamCoeff { get; set; }

        public void ProcessTurn(GameContext gameContext)
        {
            Console.Error.WriteLine("{0}) Processing turn. State: {1}", this.Id, gameContext.GameState);

            this.StunRecovery -= 1;
            var finished = false;

            if (gameContext.GameState == GameState.Late && gameContext.MyBusters.Values.Any(b => b.InterseptorId > 0))
            {
                gameContext.MyBusters.Values.ToList().ForEach(b => b.InterseptorId = 0);
                Console.Error.WriteLine("----- Transforming interseptors...");
            }

            if (this.CanReleaseGhost())
            {
                this.Release();
                gameContext.MyScore++;
                Console.Error.WriteLine("{0}) Releasing ghost #{1}", this.Id, this.GhostId);
                gameContext.Ghosts.Remove(this.GhostId);
                return;
            }

            finished = this.ProcessEnemies(gameContext);

            if (!finished && this.ShouldGoBackToBase())
            {
                this.MoveTowardsBase(gameContext.GameState);
                return;
            }

            if (!finished)
            {
                finished = this.ProcessGhosts(gameContext);
            }

            if (!finished)
            {
                finished = this.ProcessHelpPoint();
            }

            if (!finished)
            {
                MoveRandomlyIfNoGhostsAreNear(gameContext);
            }
        }

        internal bool ShouldGoStraightToBase(int teamCoeff, int targetX, int targetY, GameState gameState)
        {
            if (teamCoeff == 1)
            {
                var noNeedToGoToMidPoint = this.Point.X < targetX && this.Point.Y <= targetY;
                var isOnTopBorder = this.Point.X <= targetX && this.Point.Y == 0;
                var isOnLeftBorder = this.Point.X == 0 && this.Point.Y <= targetY;
                var isTooEarlyToRunAway = gameState == GameState.Early;

                return noNeedToGoToMidPoint || isOnTopBorder || isOnLeftBorder || isTooEarlyToRunAway;
            }
            else
            {
                var noNeedToGoToMidPoint = this.Point.X >= targetX && this.Point.Y >= targetY;
                var isOnBottomBorder = this.Point.X >= targetX && this.Point.Y == Constants.MaxHeight;
                var isOnRightBorder = this.Point.X == Constants.MaxWidth && this.Point.Y >= targetY;
                var isTooEarlyToRunAway = gameState == GameState.Early;

                return noNeedToGoToMidPoint || isOnBottomBorder || isOnRightBorder || isTooEarlyToRunAway;
            }
        }

        internal Point GetCornerToMoveAtWithGhostInLateGame(Buster buster)
        {
            var distanceToTopRightCorner = Geometry.CalculateDistance(buster.Point, Constants.TopRightCorner);
            var distanceToBottomLeftCorner = Geometry.CalculateDistance(buster.Point, Constants.BottomLeftCorner);
            var targetPoint = distanceToTopRightCorner < distanceToBottomLeftCorner ? Constants.TopRightCorner : Constants.BottomLeftCorner;

            return targetPoint;

        }

        internal void MoveTowardsBase(GameState gameState)
        {
            Point point;

            var targetX = this.BasePoint.X + (this.TeamCoeff * Constants.MaxGhostBustDistance * 2);
            var targetY = this.BasePoint.Y + (this.TeamCoeff * Constants.MaxGhostBustDistance * 2);

            if (gameState == GameState.Late)
            {
                point = GetCornerToMoveAtWithGhostInLateGame(this);
            }
            //else
            //{
            //    var optimalXandY = Geometry.GetLegsOfRightTriangle(Constants.MaxDistanceFromBaseToReleaseGhost);
            //    point = this.BasePoint
            //        .AddX(this.TeamCoeff * optimalXandY)
            //        .AddY(this.TeamCoeff * optimalXandY);
            //}
            else if (ShouldGoStraightToBase(this.TeamCoeff, targetX, targetY, gameState))
            {
                point = this.BasePoint;
            }
            else
            {
                point = this.GetBaseMidPoints(this.BasePoint, targetX, targetY);
            }

            Console.Error.WriteLine("{0}) Moving towards base: {1}", this.Id, point);
            this.Move(point);
        }

        private Point GetBaseMidPoints(Point basePoint, int targetX, int targetY)
        {
            Point point;
            if (this.TeamId == 0)
            {
                if (this.Point.X > targetX)
                {
                    point = new Point(targetX, basePoint.Y);
                }
                else
                {
                    point = new Point(basePoint.X, targetY);
                }
            }
            else
            {
                if (this.Point.X < targetX)
                {
                    point = new Point(targetX, basePoint.Y);
                }
                else
                {
                    point = new Point(basePoint.X, targetY);
                }
            }

            return point;
        }

        internal void InitializeMovingPoint()
        {
            if (this.Position == 1)
            {
                this.MovingPoint = this.OppositeTeamBase
                    .AddX(this.TeamCoeff * -Constants.OptimalXandYFromCornerToCatchGhosts)
                    .AddY(this.TeamCoeff * -Constants.OptimalXandYFromCornerToCatchGhosts);
            }
            else if (this.Position % 2 == 0)
            {
                this.MovingPoint = this.TeamId == 0 ?
                    Constants.TopRightCorner
                        .AddX(-Constants.OptimalXandYFromCornerToCatchGhosts)
                        .AddY(Constants.OptimalXandYFromCornerToCatchGhosts) :
                    Constants.BottomLeftCorner
                        .AddX(Constants.OptimalXandYFromCornerToCatchGhosts)
                        .AddY(-Constants.OptimalXandYFromCornerToCatchGhosts);
            }
            else
            {
                this.MovingPoint = this.TeamId == 0 ?
                    Constants.BottomLeftCorner
                        .AddX(Constants.OptimalXandYFromCornerToCatchGhosts)
                        .AddY(-Constants.OptimalXandYFromCornerToCatchGhosts) :
                    Constants.TopRightCorner
                        .AddX(-Constants.OptimalXandYFromCornerToCatchGhosts)
                        .AddY(Constants.OptimalXandYFromCornerToCatchGhosts);
            }

            this.PreviousMovingPoint = this.BasePoint;

            Console.Error.WriteLine("{0}) Initialized moving point: {1}", this.Id, this.MovingPoint);
        }

        internal bool ProcessGhosts(GameContext gameContext)
        {
            var sortedGhosts = gameContext.VisibleGhosts.Values.OrderBy(g => g.Stamina);
            var gameState = gameContext.GameState;

            foreach (var ghost in sortedGhosts)
            {
                if (this.IsGhostInBustingRange(ghost) &&
                    this.ShouldBustGhost(ghost, gameState))
                {
                    Console.Error.WriteLine("{0}) Busting ghost... {1}", this.Id, ghost.Stamina);

                    this.Bust(ghost.Id);
                    var ghostBustIndex = this.NumberOfBustersBustingGhost(gameContext.MyBusters, ghost) - this.NumberOfBustersBustingGhost(gameContext.EnemyBusters, ghost);
                    var buster = gameContext.MyBusters.Values.Where(b => b.Id != this.Id).FirstOrDefault(b => b.HelpPoint == null);
                    if (ghost.Stamina <= ghost.BustersCount &&
                         ghostBustIndex != 0 &&
                        (gameState == GameState.MidLate || gameState == GameState.Late))
                    {
                        if (buster != null)
                        {
                            Console.Error.WriteLine("{0}) Notifying buster #{1}", this.Id, buster.Id);
                            if (gameState == GameState.Late)
                            {
                                buster.HelpPoint = this.GetCornerToMoveAtWithGhostInLateGame(this);
                            }
                            else if (ghostBustIndex > 0)
                            {
                                buster.HelpPoint = this.BasePoint
                                    .AddX(this.TeamCoeff * Constants.MaxGhostBustDistance)
                                    .AddY(this.TeamCoeff * Constants.MaxGhostBustDistance);
                            }
                            else
                            {
                                buster.HelpPoint = this.OppositeTeamBase
                                    .AddX(this.TeamCoeff * -Constants.MaxGhostBustDistance)
                                    .AddY(this.TeamCoeff * -Constants.MaxGhostBustDistance);
                            }
                        }

                        gameContext.MyBusters.Values
                            .Where(b => b.HelpPoint != null && Geometry.IsSamePoint(b.HelpPoint, ghost.Point))
                            .ToList().ForEach(b => b.HelpPoint = null);
                    }
                    else if (ghostBustIndex == 0 && buster != null)
                    {
                        buster.HelpPoint = ghost.Point;
                    }

                    this.HelpPoint = null;
                    return true;
                }
                else if (this.IsTooCloseToGhost(ghost))
                {
                    this.Move(this.BasePoint);
                    return true;
                }
                else if (this.HelpPoint == null &&
                    this.ShouldBustGhost(ghost, gameState) &&
                    (this.CalculateTurnsToReachPoint(ghost.Point) / (ghost.BustersCount == 0 ? 1 : ghost.BustersCount)) < ghost.Stamina &&
                    this.IsInRange(ghost.Point, Constants.MaxHelpDistance))
                {
                    Console.Error.WriteLine("{0}) Setting help point: {1}", this.Id, ghost.Point);

                    this.HelpPoint = ghost.Point;
                }
            }

            return false;
        }

        internal bool ProcessEnemies(GameContext gameContext)
        {
            foreach (var buster in gameContext.VisibleEnemies.Values)
            {
                if (this.CanStunEnemyBuster(buster, gameContext.Ghosts, gameContext.GameState))
                {
                    this.Stun(buster.Id);
                    buster.State = State.Stunned;
                    //this.HelpPoint = buster.Point;
                    Console.Error.WriteLine("{0}) Stunning buster #{1}", this.Id, buster.Id);

                    return true;
                }
                else if (this.CanInterseptEnemyBuster(buster, gameContext.GameState))
                {
                    if (this.HelpPoint != null)
                    {
                        this.HelpPoint = CalculateInterseptionPoint(buster, gameContext.GameState);
                        Console.Error.WriteLine("{0}) Intersepting buster #{1} - {2}", this.Id, buster.Id, this.MovingPoint);
                    }
                }
            }

            return false;
        }

        internal bool ProcessHelpPoint()
        {
            if (this.HelpPoint == null)
            {
                return false;
            }
            if (this.HelpPoint != null &&
                this.IsInRange(this.HelpPoint, Constants.MaxGhostBustDistance))
            {
                this.HelpPoint = null;
                movingPoint = this.MovingPoint;
                Console.Error.WriteLine("{0}) Reached help point. Going to {1}", this.Id, movingPoint);
            }
            else
            {
                movingPoint = this.HelpPoint;
                Console.Error.WriteLine("{0}) Going to help point: {1}", this.Id, movingPoint);
            }

            Move(movingPoint);
            return true;
        }

        internal void MoveRandomlyIfNoGhostsAreNear(GameContext gameContext)
        {
            Point movingPoint;
            var gameState = gameContext.GameState;
            var sortedGhosts = gameContext.Ghosts.Values.OrderBy(g => g.Stamina).ToList();
            if (gameState != GameState.Late && gameState != GameState.MidLate)
            {
                sortedGhosts = sortedGhosts.Where(g => g.Stamina <= 15).ToList();
            }

            var targetGhost = sortedGhosts.FirstOrDefault();

            if (this.InterseptorId > 0 && (gameState == GameState.MidLate || gameState == GameState.MidEarly))
            {
                movingPoint = this.OppositeTeamBase;
                // TODO: Calculate best offsets to intersept
                var pointOffset = this.InterseptorId == 1 ? 300 : -300;
                if (this.TeamSize < 4)
                {
                    pointOffset = 0;
                }

                var baseOffset = Constants.MaxGhostBustDistance;

                var point1 = movingPoint
                            .AddX((this.TeamCoeff * -baseOffset) + pointOffset)
                            .AddY((this.TeamCoeff * -baseOffset) - pointOffset);

                movingPoint = point1;

                Console.Error.WriteLine("{0}) Intersepting at {1}", this.Id, movingPoint);
            }
            //else if (sortedGhosts.Count > 0 && !Geometry.IsSamePoint(this.Point, targetGhost.Point))
            //{
            //    Console.Error.WriteLine("Moving to target ghost! {0}) {1}", targetGhost.Id, targetGhost.Point);
            //    movingPoint = targetGhost.Point;
            //}
            else if (HasReachedACorner() || Geometry.IsSamePoint(this.Point, this.MovingPoint))
            {
                movingPoint = this.GetRandomPoint();
                this.MovingPoint = movingPoint;
                Console.Error.WriteLine("{0}) Reached corner. Going to {1}", this.Id, movingPoint);
            }
            else
            {
                movingPoint = this.MovingPoint;
                Console.Error.WriteLine("{0}) Going to {1}", this.Id, movingPoint);
            }

            Move(movingPoint);
        }

        internal Point GetRandomPoint()
        {
            var optimalDistance = Geometry.GetLegsOfRightTriangle(Constants.MaxGhostBustDistance);
            var randomPoints = new List<Point>()
            {
                Constants.Team0Base.AddX(optimalDistance).AddY(optimalDistance),
                Constants.Team1Base.AddX(-optimalDistance).AddY(-optimalDistance),
                Constants.TopRightCorner.AddX(-optimalDistance).AddY(optimalDistance),
                Constants.BottomLeftCorner.AddX(optimalDistance).AddY(-optimalDistance)
            };

            var randomPoint = randomPoints[Constants.Rand.Next(0, randomPoints.Count)];
            var needOtherRandomPoint = randomPoint == this.MovingPoint;
            while (needOtherRandomPoint)
            {
                randomPoint = randomPoints[Constants.Rand.Next(0, randomPoints.Count)];
            }

            return randomPoint;
        }

        internal Ghost GetGhostById(int id, Dictionary<int, Ghost> ghosts)
        {
            return ghosts.Values.FirstOrDefault(g => g.Id == id);
        }

        #region Commands
        internal void Move(Point point)
        {
            Console.WriteLine("MOVE {0} {1} {2}", point.X, point.Y, this.HelpPoint); // MOVE x y | BUST id | RELEASE
        }

        internal void Bust(int id)
        {
            Console.WriteLine("BUST {0} {1}", id, this.HelpPoint); // MOVE x y | BUST id | RELEASE
        }

        internal void Release()
        {
            Console.WriteLine("RELEASE {0}", this.HelpPoint); // MOVE x y | BUST id | RELEASE
        }

        internal void Stun(int id)
        {
            this.StunRecovery = Constants.TurnsUntilBusterCanStunAgain;
            Console.WriteLine("STUN {0} {1}", id, this.HelpPoint); // MOVE x y | BUST id | RELEASE
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
            return this.IsInRange(Constants.Team1Base, Constants.VisibleDistance);
        }

        internal bool HasReachedTopRightCorner()
        {
            return this.IsInRange(Constants.TopRightCorner, Constants.VisibleDistance);
        }

        internal bool HasReachedBottomLeftCorner()
        {
            return this.IsInRange(Constants.BottomLeftCorner, Constants.VisibleDistance);
        }

        internal bool HasReachedTopLeftCorner()
        {
            return this.IsInRange(Constants.Team0Base, Constants.VisibleDistance);
        }

        internal bool IsInRange(Point point, double distance)
        {
            return Geometry.CalculateDistance(this.Point, point) <= distance;
        }

        internal bool IsGhostInBustingRange(Ghost ghost)
        {
            return Geometry.CalculateDistance(this.Point, ghost.Point) >= Constants.MinGhostBustDistance &&
                Geometry.CalculateDistance(this.Point, ghost.Point) <= Constants.MaxGhostBustDistance;
        }

        internal bool ShouldBustGhost(Ghost ghost, GameState gameState)
        {
            var isToughGhost = ghost.Stamina > 15;
            var shouldBustToughGhost = isToughGhost && (gameState != GameState.Early);

            return !isToughGhost || shouldBustToughGhost;
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

        internal bool CanStunEnemyBuster(Buster buster, Dictionary<int, Ghost> ghosts, GameState gameState)
        {
            var isInConditionToStun = this.StunRecovery <= 0 &&
                buster.State != State.Stunned &&
                Geometry.CalculateDistance(this.Point, buster.Point) <= Constants.MaxGhostBustDistance;

            var shouldStunBusterWithGhost = (buster.State == State.CarryingGhost ||
                (buster.State == State.TrappingGhost && this.GetGhostById(buster.GhostId, ghosts)?.Stamina < 10)) &&
                isInConditionToStun;

            var distanceFromBase = Geometry.GetHypothenuseOfRightTriangle(Constants.VisibleDistance);
            var shouldStunBusterWithoutGhost = isInConditionToStun &&
                (Geometry.CalculateDistance(buster.Point, this.BasePoint) <= distanceFromBase ||
                gameState == GameState.Late || gameState == GameState.MidLate);

            return isInConditionToStun /*shouldStunBusterWithGhost || shouldStunBusterWithoutGhost*/;
        }

        internal bool CanInterseptEnemyBuster(Buster buster, GameState gameState)
        {
            return buster.State == State.CarryingGhost &&
                this.State != State.CarryingGhost &&
                this.StunRecovery <= this.CalculateTurnsToReachPoint(this.OppositeTeamBase) &&
                Geometry.CalculateDistance(buster.Point, this.OppositeTeamBase) > Geometry.CalculateDistance(this.Point, this.OppositeTeamBase);
        }
        #endregion

        internal int NumberOfBustersBustingGhost(Dictionary<int, Buster> busters, Ghost ghost)
        {
            return busters.Values.Where(b => b.GhostId == ghost.Id).Count();
        }

        internal Point CalculateInterseptionPoint(Buster buster, GameState gameState)
        {
            var dropPoint = gameState == GameState.Late ? this.OppositeTeamBase : this.GetCornerToMoveAtWithGhostInLateGame(buster);
            var distanceFromBusterToBase = Geometry.CalculateDistance(this.Point, dropPoint);

            var interseptionPoint = Geometry.GetPointAlongLine(dropPoint, buster.Point, distanceFromBusterToBase);
            Console.Error.WriteLine("Calculated interseption point: {0}", interseptionPoint);
            return interseptionPoint;
        }

        internal int CalculateTurnsToReachPoint(Point point)
        {
            var distance = (int)Geometry.CalculateDistance(this.Point, point);

            return distance / Constants.MaxBusterMoveDistance;
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
            if (this == null)
            {
                return "null";
            }

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

    public class GameContext
    {
        private int turn;

        public int MyTeamId { get; set; }
        public Dictionary<int, Buster> MyBusters { get; set; }
        public Dictionary<int, Buster> EnemyBusters { get; set; }
        public Dictionary<int, Ghost> Ghosts { get; set; }
        public GameState GameState { get; set; }
        public int Turn
        {
            get { return this.turn; }
            set
            {
                turn = value;
                this.GameState = DefineGameState(value);
            }
        }
        public int GhostCount { get; set; }
        public int BustersPerTeam { get; set; }
        public int MyScore { get; set; }
        public int EnemyScore { get; set; }
        public Dictionary<int, Buster> VisibleEnemies { get; set; }
        public Dictionary<int, Ghost> VisibleGhosts { get; set; }

        private GameState DefineGameState(int turn)
        {
            if (turn < Constants.EarlyGameTurns)
            {
                return GameState.Early;
            }
            else if (turn < Constants.MidEarlyGameTurns)
            {
                return GameState.MidEarly;
            }
            else if (turn < Constants.MidLateGameTurns)
            {
                return GameState.MidLate;
            }
            else
            {
                return GameState.Late;
            }
        }
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