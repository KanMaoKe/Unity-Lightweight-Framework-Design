using System;

namespace LiteFW
{
    /// <summary>
    /// C# 原生 Tag 数据源：由业务项目提供标签路径。
    /// </summary>
    public sealed class NativeGameplayTagSource : IGameplayTagSource
    {
        private readonly string[] m_Paths;

        /// <summary>用标签路径创建数据源。</summary>
        public NativeGameplayTagSource(params string[] paths)
        {
            m_Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>将路径注册到标签管理器。</summary>
        public void Collect(IGameplayTagRegistration registration)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            for (int i = 0; i < m_Paths.Length; i++)
                registration.Register(m_Paths[i]);
        }
    }
}
