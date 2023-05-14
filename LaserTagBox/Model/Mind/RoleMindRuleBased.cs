using System;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Body;
using LaserTagBox.Model.Shared;
using LaserTagBox.Model.Spots;
using Mars.Common.Core.Random;
using Mars.Interfaces.Environments;

namespace LaserTagBox.Model.Mind;

public class RoleMindRuleBased : AbstractPlayerMind
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
        switch (Body.MemberID())
        {
            case 0 : role = Role.Shooter; break;
            case 1 : role = Role.Scouter; break;
            case 2 : role = Role.Assister; break;
        }
    }
    #endregion
    
    #region tick
    public override void Tick()
    {
        if (Body.ActionPoints >= 30)
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
            DoDefensiveStrategy();
        }
        
    }
    #endregion
    
    #region Methods
    
    private void DoAggresiveStrategyForShooter()
    {
        if (!_goForwardShooter)
        {
            RandomMove();
            _shooterPosition = Body.Position;
            return;
        }

        _goForwardShooter = false;
        if (!GoForShot())
        {
            RandomMove();
        }
    }
    
    private void DoAggresiveStrategyForScouter()
    {
        var hills = Body.ExploreHills1();
        if (hills.Count > 0)
        {
            Body.GoTo(hills.OrderBy(x => Body.GetDistance(x)).FirstOrDefault());
            enemies = Body.ExploreEnemies1();
            if (enemies.Any())
            {
                _enemy = enemies.First();
                _enemyPosition = _enemy.Position.Copy();
                TellAssister();
                TellShooter();
                bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
                if (Body.GetDistance(_enemyPosition) <= 5 && successRateForShooting)
                {
                    if (Body.Stance != Stance.Lying)
                    {
                        Body.ChangeStance2(Stance.Lying);
                    }
                    
                    if (Body.RemainingShots == 0)
                    {
                        Body.Reload3();
                
                    }

                    Body.Tag5(_enemyPosition);
                    MoveAgentAfterShooting();
                }
            }
        }
        else
        {
            RandomMove();
        }
    }
    
    private void DoAggresiveStrategyForAssister()
    {
        if (!_goForwardAssister)
        {
            Body.GoTo(_shooterPosition);
            return;
        }
        _goForwardAssister = false;
        if (!GoForShot())
        {
            RandomMove();
        }
    }
    
    private void DoDefensiveStrategy()
    {
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

    private void TellShooter()
    {
        _goForwardShooter = true;
    }
    
    private void TellAssister()
    {
        _goForwardAssister = true;
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
                if (Body.Stance != Stance.Lying)
                {
                    Body.ChangeStance2(Stance.Lying);
                }

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
    private Position _enemyPosition;
    private Position _shooterPosition;
    private EnemySnapshot _enemy;
    private List<EnemySnapshot> enemies;
    private List<PlayerBody> teammates;
    private Role role;
    private bool _goForwardShooter = false;
    private bool _goForwardAssister = false;
}