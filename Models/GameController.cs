using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using AutoFilterPresets.Setings.Models;

namespace AutoFilterPresets.Views
{
    public enum ControllerInput
    {
        None,
        Start,
        Back,
        LeftStick,
        RightStick,
        LeftShoulder,
        RightShoulder,
        Guide,
        A,
        B,
        X,
        Y,
        DPadLeft,
        DPadRight,
        DPadUp,
        DPadDown,
        TriggerLeft,
        TriggerRight,
        LeftStickLeft,
        LeftStickRight,
        LeftStickUp,
        LeftStickDown,
        RightStickLeft,
        RightStickRight,
        RightStickUp,
        RightStickDown
    }
    public static class GameController
    {
        static Assembly playnite;
        static Type PinGameControllerInputBinding;
        static Type PinControllerInput;
        static ConstructorInfo GameControllerInputBinding_ctor;
        static Type PinGameControllerGesture;
        static dynamic Controllers;

        static GameController()
        {
            playnite = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playnite.dll"));
            PinGameControllerInputBinding = playnite.GetType("Playnite.Input.GameControllerInputBinding");
            PinControllerInput = playnite.GetType("Playnite.Input.ControllerInput");
            PinGameControllerGesture = playnite.GetType("Playnite.Input.GameControllerGesture");
            GameControllerInputBinding_ctor = PinGameControllerInputBinding.GetConstructor(new Type[] { typeof(ICommand), PinControllerInput });

            dynamic model = Application.Current.MainWindow.DataContext;
            Controllers = model.App.GameController.Controllers;
        }

        public static InputBinding CreateInputBinding(ControllerInput button, ICommand command, object commandParameter=null)
        {
            var input = PinControllerInput.GetField(button.ToString())?.GetValue(null);
            var binding = GameControllerInputBinding_ctor.Invoke(new object[] { command, input }) as InputBinding;
            if (commandParameter != null)
            {
                binding.CommandParameter = commandParameter;
            }
            return binding;
        }

        public static ControllerInput ConfirmationBinding => (ControllerInput)(PinGameControllerGesture
                .GetProperty("ConfirmationBinding", BindingFlags.Public|BindingFlags.Static)
                ?.GetValue(null) ?? ControllerInput.None);
        public static ControllerInput CancellationBinding => (ControllerInput)(PinGameControllerGesture
                .GetProperty("CancellationBinding", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) ?? ControllerInput.None);

        static int GetControllesStateHash()
        {
            int combinedHash = 17;
            try
            {
                foreach (var controller in Controllers)
                {
                    foreach (var kvp in controller.LastInputState)
                    {
                        int keyHash = kvp.Key.GetHashCode();
                        int valueHash = kvp.Value.GetHashCode();
                        unchecked // Allow arithmetic overflow, numbers will "wrap around"
                        {
                            combinedHash = combinedHash * 23 + keyHash;
                            combinedHash = combinedHash * 23 + valueHash;
                        }
                    }
                }
            }
            catch { }

            return combinedHash;
        }
        public static void LongPressCommand( ICommand shortCommand, ICommand longCommand, object parameter )
        {
            var startState = GetControllesStateHash();
            DateTime startTime = DateTime.Now;
            System.Timers.Timer timer = new System.Timers.Timer(50)
            {
                AutoReset = true,
                Enabled = true
            };

            timer.Elapsed += (sender, e) =>
            {
                if (startState != GetControllesStateHash())
                {
                    timer.Stop();
                    timer.Dispose();
                    shortCommand.Execute(parameter);
                }
                else if ((DateTime.Now - startTime).TotalMilliseconds > 500)
                {
                    timer.Stop();
                    timer.Dispose();
                    longCommand.Execute(parameter);
                }
            };
        }
    }
}
