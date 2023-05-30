using System;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Shared;
using Mars.Common.Core.Random;
using Mars.Interfaces.Environments;
using ServiceStack;
// ReSharper disable All
namespace LaserTagBox.Model.Mind;

public class RuleBasedMind : AbstractPlayerMind
{
    #region Fields

    private PlayerMindLayer _mindLayer;
    private static Guid _shooter;
    private static Guid _assistant;
    private static Guid _scouter;
    private Position _enemyPosition;
    private EnemySnapshot _enemy;
    private List<EnemySnapshot> _enemies;
    private Role _role;
    private bool _isScouterDead = false;
    private bool _isShooterDead = false;
    private bool _isScouterLow = false;
    private bool _isShooterLow = false;
    //Tracked ob irgendein Agent aus dem eigenen Team auf einem hill befindet
    private bool _isScouterOnHill = false;
    private bool _isScouterBeforeOnHill = false;
    //Zählt wie oft sich pro Tick in einer Simulation ein Agent aus dem eigenen Team auf einem hill befindet 
    private int _tickOnTheHill;
    private int _tickAfterOnTheHill;
   #endregion
    
    
    /**
     *  Im Lasertagteam hat jeder Agent eine spezielle Rolle.
     *  Diese Rollen werden durch das Enum Role vergeben
     */
    private enum Role
    {
        Shooter, Scouter, Assistant
    }

    #region init
    /**
     *  Hier findet die Iniatilisierung des Rules-Based-Agent statt.
     *  Jeder der drei Agents bekommt eine eigene Rolle aus dem enum Role und
     *  eine ID
     */
    public override void Init(PlayerMindLayer mindLayer)
    {
        _mindLayer = mindLayer;
        if (_shooter == Guid.Empty)
        {
            //Console.WriteLine("Shooter init");
            _shooter = this.ID;
            _role = Role.Shooter;
        } else if (_assistant == Guid.Empty)
        {
            //Console.WriteLine("Assistant init");
            _assistant = this.ID;
            _role = Role.Assistant;
        } else if (_scouter == Guid.Empty)
        {
            //Console.WriteLine("Scouter init");
            _scouter = this.ID;
            _role = Role.Scouter;
        }
    }
    #endregion
    
    #region tick
    public override void Tick()
    {
        if (_mindLayer.GetCurrentTick() < 50)
        {
            if (!GoForShot())
            {
                RandomMove();
            }
        }else if (Body.Energy >= 20)
        {
            switch (_role)
            {
                case Role.Shooter:
                    _isShooterLow = false;
                    DoAggressiveStrategyForShooter(); 
                    break;
                case Role.Scouter:
                    _isScouterLow = false;
                    DoAggressiveStrategyForScouter();
                    break;
                case Role.Assistant:
                    DoAggressiveStrategyForAssistant();
                    break;
            }
        }
        else
        {
            //ist die Energy kleiner < 30, soll eine allgemeine defensive Strategie verfolgt werden
            switch (_role)
            {
                case Role.Shooter:
                    _isShooterLow = true; break;
                case Role.Scouter:
                    _isScouterLow = true; break;
            }
            DoDefensiveStrategy();
        }
    }
    #endregion
    
    #region Methods
    
    private void DoAggressiveStrategyForShooter()
    {
        if (!GoForShot())
        {
            RandomMove();
        }
    }
    
    private void DoAggressiveStrategyForScouter()
    {
        //es befindet sich noch kein Agent aus dem eigenen Team auf einem hill 
        if(!_isScouterOnHill && !_isScouterBeforeOnHill)
        {
            _tickAfterOnTheHill = 0;
            var hills = Body.ExploreHills1();
            if (hills != null && hills.Count > 0)
            {
                //gehe zum nähesten hill 
                Body.GoTo(hills.OrderBy(x => Body.GetDistance(x)).FirstOrDefault());
                if (Body.GetDistance(hills.OrderBy(x => Body.GetDistance(x)).FirstOrDefault()) == 0)
                {
                    _isScouterOnHill = true;
                    _tickOnTheHill = 0;
                }
                //Scouter ist nun auf einem hill, somit können entsprechende Aktionen ausgeführt werden
                ActionOnTheHills();
            } else
            {
                //kein hill befindet sich in Sichtweite, Agent soll sich random bewegen. 
                RandomMove();
            }
        }
        //es befindet sich schon ein Agent auf irgendeinem einem hill
        else if (_tickAfterOnTheHill < 20 && _isScouterBeforeOnHill)
        {
            _tickAfterOnTheHill++;
            RandomMove();
            GoForShot();
            if (_tickAfterOnTheHill == 20)
            {
                _isScouterBeforeOnHill = false;
            }
        }else 
        {
            ActionOnTheHills();
            _tickOnTheHill++;
            if ((_tickOnTheHill % 5 == 0 )|| Body.WasTaggedLastTick)
            {
                RandomMove();
                _isScouterOnHill = false;
                _isScouterBeforeOnHill = true;
            }
        }
    }
    
    private void DoAggressiveStrategyForAssistant()
    {
        if (!_isScouterDead && !_isShooterDead && !_isScouterLow && !_isShooterLow)
        {
            if (!GoForShot())
            {
                RandomMove();
            }
        } else if (_isScouterDead || _isScouterLow)
        {
            DoAggressiveStrategyForScouter();
        } else if (_isShooterDead || _isShooterLow) 
        {
            DoAggressiveStrategyForShooter();
        }
    }
    
    private void DoDefensiveStrategy()
    {
        //aktuallisiere zunächst den Alive Zustand von der aktuellen Agent-Role 
        switch (_role)
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
        }
        //erforsche das Spielfeld nach Barieren und Gräben
        //var exploreBarriers1 = Body.ExploreBarriers1(); 
        var exploreDitches1 = Body.ExploreDitches1();
        //gibt es mind. 1 Barriere in dem Sichtfeld des Agenten, dann gehe dahin und versuche einen Shot
        /*
        if (exploreBarriers1 != null && exploreBarriers1.Any())
        {
            Body.GoTo(exploreBarriers1[0]);
        }//gibt es mind. 1 Graben in dem Sichtfeld des Agenten, dann gehe dahin und versuche einen Shot 
        else 
        */
        
        if (exploreDitches1 != null && exploreDitches1.Any())
        {
            //var goal = exploreDitches1.OrderBy(x => Body.GetDistance(x)).FirstOrDefault();
            /*if (Body.GetDistance(goal) == 0)
            {
                if (Body.Stance != Stance.Lying)
                {
                    Body.ChangeStance2(Stance.Lying);
                }
            }
            else
            {
                if (Body.Stance != Stance.Standing)
                {
                    Body.ChangeStance2(Stance.Standing);
                }
            }*/
            Body.GoTo(exploreDitches1[0]);
        }


        if (!GoForShot())
        {
            RandomMove();
        }


    }
    
    /**
     *  Diese Methode wird ausgeführt wenn sich ein Agent aus dem eigenen Team auf einem hill befindet.
     *  Die Position auf dem hill wird genutzt um den Agenten der Rollen Assistant und Shooter Informationen über einen
     *  Enemy zu geben. 
     *  Wenn die Action on hill erfolgreich war dann liefert die Methode true ansonsten false. 
     */
    private void ActionOnTheHills()
    {
        //suche nach enemies 
        _enemies = Body.ExploreEnemies1();
        GoForShot();
    }
    
    /**
     *  Diese Methode dient dazu das ein Agent einen Gegner versucht zu taggen.
     *  Dabei wird mit einer Success-Rate gearbeitet wie erfolgreich der Tag-Versuch ist. 
     */
    private bool GoForShot()
    {
        /*
        if (_enemies == null)
        {
            _enemies = Body.ExploreEnemies1();
        }
        */
        _enemies = Body.ExploreEnemies1();
        //gibt es mind. 1 Gegner in der Umgebung des Agents
        if (_enemies != null && _enemies.Any())
        {
            _enemy = _enemies.First();
            _enemyPosition = _enemy.Position.Copy();
            //bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
            var hasBeeLine = Body.HasBeeline1(_enemyPosition);
            if (hasBeeLine)
            {
                if (Body.GetDistance(_enemyPosition) <= 5)
                {
                    if (Body.Stance != Stance.Lying)
                    {
                        //erhoeht die Wahrscheinlichkeit das der Gegner erfolgreich getagged wird 
                        Body.ChangeStance2(Stance.Lying);
                    }
                }
                else if (Body.GetDistance(_enemyPosition) <= 8)
                {
                    if (Body.Stance != Stance.Kneeling)
                    {
                        Body.ChangeStance2(Stance.Kneeling);
                    }
                } else if(Body.GetDistance(_enemyPosition) <= 10)
                {
                    if (Body.Stance != Stance.Standing)
                    {
                        Body.ChangeStance2(Stance.Standing);
                    }
                }
                if (Body.RemainingShots == 0)
                {
                    Body.Reload3();
                    Body.ChangeStance2(Stance.Lying);
                }
                else
                {
                    Body.Tag5(_enemyPosition);
                }
                
                RandomMove();
                return true;
            }
        }
        return false; 
    }
    
    /**
     *  Die Methode dient dazu einen Agenten an eine zufällige Position
     *  zuführen. 
     */
    private void RandomMove()
    {
        var x = RandomHelper.Random.Next(51);
        var y = RandomHelper.Random.Next(51);

        var goal = Position.CreatePosition(x, y);
        Body.ChangeStance2(Stance.Standing);
        Body.GoTo(goal);
    }
    #endregion
}