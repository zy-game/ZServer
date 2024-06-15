using System;
using System.Text;
using Newtonsoft.Json;

namespace ZGame.Networking
{
    /// <summary>
    /// 消息数据
    /// </summary>
    public interface IMessage : IReference
    {
        /// <summary>
        /// 序列化消息
        /// </summary>
        /// <param name="writer"></param>
        void Encode(BinaryWriter writer);

        /// <summary>
        /// 反序列化消息
        /// </summary>
        /// <param name="reader"></param>
        void Decode(BinaryReader reader);
    }
}