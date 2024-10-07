
class Weapon {
    public string Name {get;}
    public string Desc {get;}
    public float BulletDamage {get;}
    /// <summary>Amount of rounds fire per second</summary>
    public float RateOfFire {get;} 
    /// <summary>Seconds to reload</summary>
    public float ReloadTime {get;}
    /// <summary>Rounds in the magazine</summary>
    public int MagSize {get;}
    /// <summary>Rounds in reserve</summary>
    public int ReserveSize {get;}
    /// <summary>Is the weapon automatic?</summary>
    public bool Automatic {get;}
    /// <summary>Is the weapon held like a rifle, or a pistol?</summary>
    public string Stance {get;}
     
    public Weapon (Godot.Collections.Dictionary<string, string> jsonObj)  {
        Name = jsonObj["name"];
        Desc = jsonObj["desc"];
        BulletDamage = float.Parse(jsonObj["dmg"]);
        RateOfFire = float.Parse(jsonObj["rof"]);
        ReloadTime = float.Parse(jsonObj["reload"]);
        MagSize = int.Parse(jsonObj["magSize"]);
        ReserveSize = int.Parse(jsonObj["reserveSize"]);
        Automatic = bool.Parse(jsonObj["automatic"]);
        Stance = jsonObj["stance"];
    }

    public override string ToString() {
        return $"Weapon(name={Name}, desc={Desc}, bulletDmg={BulletDamage}, rof={RateOfFire}, reloadTime={ReloadTime}, magSize={MagSize}, reserveSize={ReserveSize})";
    }
}