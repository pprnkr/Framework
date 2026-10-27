using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Nyonyo.Framework.Debugging
{
    /// <summary>
    /// 디버그 명령 목록과 실행입니다. <see cref="DebugConsole"/>가 사용하며, 직접 만들어 테스트할 수도 있습니다.
    /// </summary>
    public sealed class DebugCommandRegistry
    {
        private sealed class Command
        {
            public string Name;
            public string Description;
            public string Usage;
            public Func<string[], string> Handler;
        }

        private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);

        public DebugCommandRegistry()
        {
            Register("help", "명령 목록을 보여 줍니다.", _ => Help());
        }

        public IEnumerable<string> Names => _commands.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 명령을 등록합니다. 같은 이름이 있으면 바꿉니다.
        /// </summary>
        /// <param name="handler">입력한 인자를 받아 콘솔에 표시할 결과를 돌려줍니다. 표시할 게 없으면 null을 돌려주세요.</param>
        public void Register(string name, string description, Func<string[], string> handler, string usage = null)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace))
                throw new ArgumentException($"명령 이름 '{name}'에는 공백을 쓸 수 없습니다.", nameof(name));

            _commands[name] = new Command
            {
                Name = name,
                Description = description ?? "",
                Usage = usage ?? name,
                Handler = handler ?? throw new ArgumentNullException(nameof(handler)),
            };
        }

        public void Register(string name, string description, Action<string[]> handler, string usage = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Register(name, description, args =>
            {
                handler(args);
                return null;
            }, usage);
        }

        public bool Unregister(string name) => _commands.Remove(name);

        /// <summary>
        /// 어셈블리에서 <see cref="DebugCommandAttribute"/>가 붙은 static 메서드를 찾아 등록합니다.
        /// </summary>
        /// <returns>등록한 명령 수</returns>
        public int RegisterAttributes(IEnumerable<Assembly> assemblies)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var count = 0;
            foreach (var assembly in assemblies)
            {
                foreach (var type in GameAssemblies.GetTypes(assembly))
                {
                    foreach (var method in type.GetMethods(flags))
                    {
                        var attribute = method.GetCustomAttribute<DebugCommandAttribute>();
                        if (attribute == null)
                            continue;

                        Register(attribute.Name, attribute.Description, CreateHandler(method), Usage(attribute.Name, method));
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 한 줄을 실행하고 콘솔에 표시할 결과를 돌려줍니다. 예외가 나도 던지지 않고 오류 메시지로 돌려줍니다.
        /// </summary>
        public string Execute(string line)
        {
            var tokens = Tokenize(line);
            if (tokens.Length == 0)
                return null;

            if (!_commands.TryGetValue(tokens[0], out var command))
                return $"알 수 없는 명령: {tokens[0]} (help로 목록을 확인하세요)";

            try
            {
                return command.Handler(tokens.Skip(1).ToArray());
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                return $"오류: {e.InnerException.Message}";
            }
            catch (Exception e)
            {
                return $"오류: {e.Message}\n사용법: {command.Usage}";
            }
        }

        /// <summary>
        /// 공백으로 인자를 나눕니다. 큰따옴표로 감싸면 공백이 들어간 인자를 하나로 씁니다.
        /// </summary>
        internal string[] Tokenize(string line)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(line))
                return tokens.ToArray();

            var current = new StringBuilder();
            var inQuotes = false;
            var hasToken = false;
            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (hasToken)
                        tokens.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
                else
                {
                    current.Append(c);
                    hasToken = true;
                }
            }

            if (hasToken)
                tokens.Add(current.ToString());
            return tokens.ToArray();
        }

        private string Help()
        {
            var builder = new StringBuilder();
            foreach (var name in Names)
            {
                var command = _commands[name];
                builder.Append(command.Usage);
                if (command.Description.Length > 0)
                    builder.Append("  - ").Append(command.Description);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private Func<string[], string> CreateHandler(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                return args => Format(method.Invoke(null, new object[] { args }));

            return args =>
            {
                var values = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (i < args.Length)
                        values[i] = Convert(args[i], parameters[i].ParameterType);
                    else if (parameters[i].HasDefaultValue)
                        values[i] = parameters[i].DefaultValue;
                    else
                        throw new ArgumentException($"인자 {parameters[i].Name}이(가) 필요합니다.");
                }

                return Format(method.Invoke(null, values));
            };
        }

        private string Format(object result) => result?.ToString();

        private object Convert(string value, Type type)
        {
            var culture = CultureInfo.InvariantCulture;
            if (type == typeof(string))
                return value;
            if (type == typeof(int))
                return int.Parse(value, culture);
            if (type == typeof(long))
                return long.Parse(value, culture);
            if (type == typeof(float))
                return float.Parse(value, culture);
            if (type == typeof(double))
                return double.Parse(value, culture);
            if (type == typeof(bool))
                return value == "1" || (value != "0" && bool.Parse(value));
            if (type.IsEnum)
                return Enum.Parse(type, value, true);

            throw new ArgumentException($"{type.Name} 타입 인자는 지원하지 않습니다.");
        }

        private string Usage(string name, MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                return $"{name} [args...]";

            var builder = new StringBuilder(name);
            foreach (var parameter in parameters)
            {
                builder.Append(parameter.HasDefaultValue ? " [" : " <")
                    .Append(parameter.Name).Append(':').Append(TypeName(parameter.ParameterType))
                    .Append(parameter.HasDefaultValue ? "]" : ">");
            }

            return builder.ToString();
        }

        private string TypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            return type.Name;
        }
    }
}
