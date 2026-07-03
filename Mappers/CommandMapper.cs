using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Mappers;

/// <summary>Extension methods for creating <see cref="CommandExt"/> from DTOs.</summary>
public static class CommandMapper
{
    /// <summary>Creates a door command with the specified instruction.</summary>
    public static CommandExt ToCommandExt(this byte doorCommandInstruction, int? controllerAddress = null, byte? localDoor = null)
    {
        var command = new CommandExt
        {
            Type = (uint)ACTCommandType.Door,
            DoorCommandInstruction = doorCommandInstruction
        };

        if (controllerAddress.HasValue)
            command.Controller = controllerAddress.Value;

        if (localDoor.HasValue)
            command.Door = localDoor.Value;

        return command;
    }
}
