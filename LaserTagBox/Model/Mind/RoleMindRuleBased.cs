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
            switch (role)
            {
                case Role.Shooter:
                    DoDefensiveStrategyForShooter(); break;
                case Role.Scouter:
                    DoDefensiveStrategyForScouter(); break;
                case Role.Assister:
                    DoDefensiveStrategyForAssister(); break;
            }
        }
        /*if (Body.ActionPoints < 10)
        {
            
            return;  //TODO execution order fix
        }*/
        var enemies = Body.ExploreEnemies1();
        if (enemies.Any())
        {
            _goal = enemies.First().Position.Copy();
            if (Body.RemainingShots == 0) Body.Reload3();
            Body.Tag5(enemies.First().Position);
        }
            
        if (_goal == null || Body.GetDistance(_goal) == 1)
        {
            var newX = RandomHelper.Random.Next(_mindLayer.Width);
            var newY = RandomHelper.Random.Next(_mindLayer.Height);
            _goal = Position.CreatePosition(newX, newY);
        }

        var moved = Body.GoTo(_goal);
        if (!moved) _goal = null;

    }
    #endregion
    
    #region Methods
    
    private void DoAggresiveStrategyForShooter()
    {
        while (!_goForwardShooter)
        {
            //Do Nothing, just wait for command from Scouter
        }

        _goForwardShooter = false;
        bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
        if (Body.GetDistance(_enemyPosition) <= 5)
        {
            Body.ChangeStance2(Stance.Lying);
            if (Body.RemainingShots == 0)
            {
                Body.Reload3();
                
            }
            Body.Tag5(_enemyPosition);
        }
    }
    
    private void DoAggresiveStrategyForScouter()
    {
        hills = Body.ExploreHills1();
        if (hills.Count > 0)
        {
            Body.GoTo(hills.OrderBy(x => Body.GetDistance(x)).FirstOrDefault());
            enemies = Body.ExploreEnemies1();
            if (enemies.Count > 0)
            {
                _enemy = enemies.First();
                _enemyPosition = _enemy.Position;
            }
        }
    }
    
    private void DoAggresiveStrategyForAssister()
    {
        while (!_goForwardAssister)
        {
            //Do Nothing, just wait for command from Scouter
        }
        _goForwardAssister = false;
        bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
        if (Body.GetDistance(_enemyPosition) <= 5)
        {
            Body.ChangeStance2(Stance.Lying);
            if (Body.RemainingShots == 0)
            {
                Body.Reload3();
                
            }
            Body.Tag5(_enemyPosition);
        }
    }
    
    private void DoDefensiveStrategyForShooter()
    {
        
    }
    private void DoDefensiveStrategyForScouter()
    {
        
    }
    private void DoDefensiveStrategyForAssister()
    {
        
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
        return enemy.Stance != Stance.Lying ? true : false;
    }
    
    
    #endregion
    
    private PlayerMindLayer _mindLayer;
    private Position _goal;
    private Position _enemyPosition;
    private EnemySnapshot _enemy;
    private List<Position> hills;
    private List<EnemySnapshot> enemies;
    private List<Position> ditches;
    private List<Position> barriers;
    private List<PlayerBody> teammates;
    private Role role;
    private bool _goForwardShooter = false;
    private bool _goForwardAssister = false;
}