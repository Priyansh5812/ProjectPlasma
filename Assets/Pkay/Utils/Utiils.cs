using Unity.VisualScripting;
using UnityEngine;

namespace Pkay.Utils
{
    public enum PrintStream
    {
        LOG,
        WARNING,
        ERROR
    }

    public struct Utils
    {

        public static void Print(string message, Color color, PrintStream stream = PrintStream.LOG, bool bold = false, Object context = null)
        {
            if (bold)
                message = "<b>" + message + "<b>";

            if (color != Color.clear)
            {
                message = $"<color=#{color.ToHexString()}>" + message + "</color>";
            }

            switch (stream)
            {
                case PrintStream.LOG:
                    if (context != null)
                        Debug.Log(message, context);
                    else
                        Debug.Log(message);
                    break;
                case PrintStream.WARNING:
                    if (context != null)
                        Debug.LogWarning(message, context);
                    else
                        Debug.LogWarning(message);
                    break;
                case PrintStream.ERROR:
                    if (context != null)
                        Debug.LogError(message, context);
                    else
                        Debug.LogError(message);
                    break;
                default:
                    Debug.LogError($"Invalid Print Stream {stream}");
                    break;
            }


        }
    }

}