using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elisra.Infra.CommunicationShared
{
    public interface ICommunicationSerialiable
    {
        void Serialize(BinaryWriter bw);

        void Deserialize(BinaryReader br);
    }
}
