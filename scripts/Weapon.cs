// Weapon.cs
//
// Implements the Weapon dataclass.

using System;

/// <summary>
/// Represents the stance required for holding the weapon.
/// </summary>
public enum WeaponStance {
    Rifle,
    Pistol,
    Melee,
    None
}

class Weapon {
    /// <summary>The name of the weapon.</summary>
    public string Name {get;}

    /// <summary>A description of the weapon.</summary>
    public string Description {get;}

    /// <summary>The damage caused by each bullet.</summary>
    public float BulletDamage {get;}

    /// <summary>The number of rounds fired per second.</summary>
    public float RateOfFire {get;} 

    /// <summary>The time it takes to reload the weapon, in seconds.</summary>
    public float ReloadTime {get;}

    /// <summary>The size of the magazine (the number of rounds it can hold).</summary>
    public int MagazineSize {get;}

    /// <summary>The number of rounds available in reserve.</summary>
    public int ReserveSize {get;}

    /// <summary>Indicates whether the weapon is automatic.</summary>
    public bool Automatic {get;}

    /// <summary>The stance required to hold the weapon.</summary>
    public WeaponStance Stance {get;}
     

    /// <summary>
    /// Translates a JSON object into a weapon object.
    /// </summary>
    /// <param name="JSONObj">The JSON object to translate.</param>
    public Weapon (Godot.Collections.Dictionary<string, string> JSONObj)  {
        Name = JSONObj["name"];
        Description = JSONObj["desc"];
        BulletDamage = float.Parse(JSONObj["dmg"]);
        RateOfFire = float.Parse(JSONObj["rof"]);
        ReloadTime = float.Parse(JSONObj["reload"]);
        MagazineSize = int.Parse(JSONObj["magSize"]);
        ReserveSize = int.Parse(JSONObj["reserveSize"]);
        Automatic = bool.Parse(JSONObj["automatic"]);
        Stance = Enum.TryParse(JSONObj["stance"], true, out WeaponStance stance) 
            ? stance : WeaponStance.None;
    }

    public override string ToString() {
        return $"Weapon(Name={Name}, Description={Description}, BulletDamage={BulletDamage}, RateOfFire={RateOfFire}, ReloadTime={ReloadTime}, MagazineSize={MagazineSize}, ReserveSize={ReserveSize})";
    }
}