using System;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Body;
using LaserTagBox.Model.Shared;
using LaserTagBox.Model.Spots;
using Mars.Common.Core.Random;
using Mars.Interfaces.Environments;
using ServiceStack;
namespace LaserTagBox.Model.Mind;

public class RuleBased : AbstractPlayerMind
{
    private enum Role
    {
        Shooter,
        Scouter,
        Assister
    }

    #region init
    
    public override void Init(PlayerMindLayer mindLayer)
    {
        _mindLayer = mindLayer;

        /*switch (_totalPlayer++)
        {
            case 0 : role = Role.Shooter; break;
            case 1 : role = Role.Scouter; break;
            case 2 : role = Role.Assister; break;
        }*/
        if (_shooter == Guid.Empty)
        {
            _shooter = this.ID;
            role = Role.Shooter;
        } else if (_assister == Guid.Empty)
        {
            _assister = this.ID;
            role = Role.Assister;
        } else if (_scouter == Guid.Empty)
        {
            _scouter = this.ID;
            role = Role.Scouter;
        }
    }
    #endregion
    
    #region tick
    public override void Tick()
    {
        if (Body.Energy >= 30)
        {
            switch (role)
            {
                case Role.Shooter:
                    DoAggresiveStrategyForShooter(); break;
                case Role.Scouter:
                    DoAggresiveStrategyForScouter(); break;
                case Role.Assister:
                    DoAggresiveStrategyForAssister(); break;
                    
            }

        }
        else
        {
            switch (role)
            {
                case Role.Shooter:
                    _isShooterLow = true; break;
                case Role.Scouter:
                    _isAssisterLow = true; break;
                case Role.Assister:
                    _isScouterLow = true; break;
                    
            }
            DoDefensiveStrategy();
        }
        
    }
    #endregion
    
    #region Methods

    private void DoAggresiveStrategyForShooter()
    {
        _shooterPosition = Body.Position;
        enemies = Body.ExploreEnemies1();
        if (enemies.Any())
        {
            if (Body.Stance != Stance.Standing)
            {
                Body.ChangeStance2(Stance.Standing);
            }
            GoForShot();
        }
        

        else
        {
            var exploreBarriers1 = Body.ExploreBarriers1();
            var exploreDitches1 = Body.ExploreDitches1();
            var hills = Body.ExploreHills1();
            if (exploreBarriers1 != null && exploreBarriers1.Any())
            {
                Body.GoTo(exploreBarriers1[0]);
                _shooterPosition = Body.Position;
                GoForShot();
                
            }
            else if (exploreDitches1 != null && exploreDitches1.Any())
            {
                Body.GoTo(exploreDitches1[0]);
                _shooterPosition = Body.Position;
                GoForShot();
            }
            else if (hills != null && hills.Any())
            {
                Body.GoTo(hills[0]);
                _shooterPosition = Body.Position;
                GoForShot();
            }
            else
            {
                RandomMove();
            }
        }
    }

    private void DoAggresiveStrategyForScouter()
    {
        Body.GoTo(_shooterPosition);
        
        if (enemies.Any())
        {
            if (Body.Stance != Stance.Kneeling)
            {
                Body.ChangeStance2(Stance.Kneeling);
            }
            GoForShot();
        }
        
        var exploreBarriers1 = Body.ExploreBarriers1();
        var exploreDitches1 = Body.ExploreDitches1();
        var hills = Body.ExploreHills1();
        
        if (exploreBarriers1 != null && exploreBarriers1.Any())
        {
            Body.GoTo(exploreBarriers1[0]);
            if (Body.Stance != Stance.Kneeling)
            {
                Body.ChangeStance2(Stance.Kneeling);
            }
            GoForShot();
        }
        else if (exploreDitches1 != null && exploreDitches1.Any())
        {
            Body.GoTo(exploreDitches1[0]);
            if (Body.Stance != Stance.Kneeling)
            {
                Body.ChangeStance2(Stance.Kneeling);
            }
            GoForShot();
        }
        else if (hills != null && hills.Any())
        {
            Body.GoTo(hills[0]);
            if (Body.Stance != Stance.Kneeling)
            {
                Body.ChangeStance2(Stance.Kneeling);
            }
            GoForShot();
        }
        else
        {
            RandomMove();
        }
        
        
        
    }
    
    private void DoAggresiveStrategyForAssister()
    {
        Body.GoTo(_shooterPosition);
        
        if (enemies.Any())
        {
            if (Body.Stance != Stance.Lying)
            {
                Body.ChangeStance2(Stance.Lying);
            }
            GoForShot();
        }
        
        var exploreBarriers1 = Body.ExploreBarriers1();
        var exploreDitches1 = Body.ExploreDitches1();
        var hills = Body.ExploreHills1();
        
        if (exploreBarriers1 != null && exploreBarriers1.Any())
        {
            Body.GoTo(exploreBarriers1[0]);
            if (Body.Stance != Stance.Lying)
            {
                Body.ChangeStance2(Stance.Lying);
            }
            GoForShot();
        }
        else if (exploreDitches1 != null && exploreDitches1.Any())
        {
            Body.GoTo(exploreDitches1[0]);
            if (Body.Stance != Stance.Lying)
            {
                Body.ChangeStance2(Stance.Lying);
            }
            GoForShot();
        }
        else if (hills != null && hills.Any())
        {
            Body.GoTo(hills[0]);
            if (Body.Stance != Stance.Lying)
            {
                Body.ChangeStance2(Stance.Lying);
            }
            GoForShot();
        }
        else
        {
            RandomMove();
        }

        
    }
    
    private void DoDefensiveStrategy()
    {
        switch (role)
        {
            case Role.Shooter:
                if (!Body.Alive)
                {
                    _isShooterDead = true;
                }
                break;
            case Role.Scouter:
                if (!Body.Alive)
                {
                    _isScouterDead = true;
                }
                break;
            case Role.Assister:
                if (!Body.Alive)
                {
                    _isAssisterDead = true;
                }

                break;
                    
        }
        var exploreBarriers1 = Body.ExploreBarriers1(); 
        var exploreDitches1 = Body.ExploreDitches1();
        if (exploreBarriers1 != null && exploreBarriers1.Any())
        {
            Body.GoTo(exploreBarriers1[0]);
            GoForShot();
        }
        else if (exploreDitches1 != null && exploreDitches1.Any())
        {
            Body.GoTo(exploreDitches1[0]);
            GoForShot();
        }
        else
        {
            RandomMove();
        }
        
    }
    
    /**
     * True if the enemy not lying
     */
    private bool CheckSuccessRateForShooting(EnemySnapshot enemy)
    {
        return enemy.Stance != Stance.Lying;
    }
    
    private void MoveAgentAfterShooting()
    {
        var tmpPos = Position.CreatePosition(Body.Position.X + 1, Body.Position.Y);
        Body.GoTo(tmpPos);
    }
    
    private void RandomMove()
    {
        var x = RandomHelper.Random.Next(48);
        var y = RandomHelper.Random.Next(48);

        var goal = Position.CreatePosition(x, y);
        Body.ChangeStance2(Stance.Standing);
        Body.GoTo(goal);
    }
    
    private bool GoForShot()
    {
        enemies = Body.ExploreEnemies1();
        if (enemies.Any())
        {
            _enemy = enemies.First();
            _enemyPosition = _enemy.Position.Copy();
            bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
            if (Body.GetDistance(_enemyPosition) <= 5 && successRateForShooting)
            {

                if (Body.RemainingShots == 0)
                {
                    Body.Reload3();

                }

                Body.Tag5(_enemyPosition);
                MoveAgentAfterShooting();
            }

            return true;
        }

        return false;
    }
    
    #endregion
    
    private PlayerMindLayer _mindLayer;
    private Position _goal;
    private Guid _shooter;
    private Guid _assister;
    private Guid _scouter;
    private Position _enemyPosition;
    private Position _shooterPosition;
    private EnemySnapshot _enemy;
    private List<EnemySnapshot> enemies;
    private List<PlayerBody> teammates;
    private Role role;
    private bool _goForwardShooter = false;
    private bool _goForwardAssister = false;
    private bool _isScouterDead = false;
    private bool _isShooterDead = false;
    private bool _isAssisterDead = false;
    private bool _isAssisterLow = false;
    private bool _isScouterLow = false;
    private bool _isShooterLow = false;
    private bool _aiOnTheHills = false;
    private int _tickOnTheHills;
}