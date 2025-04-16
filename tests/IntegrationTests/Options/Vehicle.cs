public abstract class Vehicle {
    public abstract bool HasWheels { get; }

    public static Vehicle Parse(string s)
        => s switch {
            "car"  => new Car(),
            "sleigh" => new Sleigh(),
            "boat" => new Boat(),
            _ => throw new Exception($"unknown vehicle '{s}'")
        };
}

public abstract class GroundVehicle : Vehicle {
    public new static GroundVehicle Parse(string s)
        => s switch {
            "car"  => new Car(),
            "sleigh" => new Sleigh(),
            _ => throw new Exception($"unknown vehicle '{s}'")
        };
}

public sealed class Car : GroundVehicle {
    public override bool HasWheels => true;
}
public sealed class Sleigh : GroundVehicle {
    public override bool HasWheels => false;
}

public sealed class Boat : Vehicle {
    public override bool HasWheels => false;
}