using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

using CommandDict = System.Collections.Generic.Dictionary<string, string>;

// public class PlannerInterface
// {
// 
//     enum CommandType { MINE, MOVE_TO, REPAIR };
// 
//     abstract class AbstractCommand
//     {
//         public int id;
//         CommandType type;
//         public AbstractCommand(CommandDict input, CommandType type_)
//         {
//             type = type_;
//             id = int.Parse(input["id"]);
//         }
// 
//         public abstract void applyCommand(GameAgent agent);
//     }
// 
//     // example: set the rover back on mining duty
//     class MineCommand : AbstractCommand
//     {
//         public MineCommand(CommandDict input) : base(input, CommandType.MINE)
//         {
//         }
// 
//         public override void applyCommand(GameAgent agent)
//         {
//             agent.target_planner.generateMiningPlan();
//         }
//     }
// 
//     // example: make the rover move to a target
//     class MoveToCommand : AbstractCommand
//     {
//         Vector2 position;
//         public MoveToCommand(CommandDict input) : base(input, CommandType.MOVE_TO)
//         {
//             float x = float.Parse(input["x"]);
//             float y = float.Parse(input["y"]);
//             position = new Vector2(x, y);
//         }
// 
//         public override void applyCommand(GameAgent agent)
//         {
//             agent.target_planner.resetPlan();
//             agent.target_planner.setRepeatPlan(false);
//             agent.target_planner.addPosition(position);
//         }
//     }
// 
//     // example: a repair command
//     class RepairCommand : AbstractCommand
//     {
//         Vector2 position;
//         float repair_duration;
//         public RepairCommand(CommandDict input) : base(input, CommandType.REPAIR)
//         {
//             float x = float.Parse(input["x"]);
//             float y = float.Parse(input["y"]);
//             position = new Vector2(x, y);
//             repair_duration = float.Parse(input["repair_time"]);
//         }
// 
//         public override void applyCommand(GameAgent agent)
//         {
//             agent.target_planner.resetPlan();
//             agent.target_planner.setRepeatPlan(false);
//             agent.target_planner.addPosition(position);
//             agent.target_planner.addDuration(repair_duration);
//         }
//     }
// 
//     private static AbstractCommand parseCommand(string command_string)
//     {
//         CommandDict command_dict = JsonConvert.DeserializeObject<CommandDict>(command_string);
//         string command_type_string = command_dict["type"];
//         CommandType command_type = (CommandType)Enum.Parse(typeof(CommandType), command_type_string);
//         AbstractCommand output_command;
// 
//         switch (command_type)
//         {
//             case CommandType.MINE:
//                 output_command = new MineCommand(command_dict);
//                 break;
//             case CommandType.MOVE_TO:
//                 output_command = new MoveToCommand(command_dict);
//                 break;
//             case CommandType.REPAIR:
//                 output_command = new RepairCommand(command_dict);
//                 break;
//             default:
//                 throw new Exception("invalid command type!");
//         }
//         return output_command;
//     }
// 
//     private static List<AbstractCommand> parseStringToCommands(string planner_input)
//     {
//         string[] commands = planner_input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
// 
//         List<AbstractCommand> command_list = new List<AbstractCommand>();
//         foreach (var next_com in commands)
//         {
//             AbstractCommand next_command = parseCommand(next_com);
//             command_list.Add(next_command);
//         }
// 
//         return command_list;
//     }
// 
//     private static void applyCommands(GameAgent[] rovers, List<AbstractCommand> commands)
//     {
//         foreach (AbstractCommand next_com in commands)
//         {
//             GameAgent agent = rovers[next_com.id];
//             next_com.applyCommand(agent);
//         }
//     }
// 
//     public static void applyInputs(GameAgent[] rovers, string planner_input)
//     {
//         List<AbstractCommand> command_list = parseStringToCommands(planner_input);
//         applyCommands(rovers, command_list);
//     }
// }
