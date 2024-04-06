using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json.Linq;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit.WorldSystem
{
    /// <summary>
    /// WorldLayer by Darkar25(i changed some parts but he showed way to better implementation)
    /// </summary>
    public class WorldLayer<T>(string filename, (int width, int height) chunks, int chunksize = 32) where T : unmanaged
    {
        private readonly int typesize = Marshal.SizeOf<T>();
        private readonly int Count = (int)Math.Pow(chunksize, 2);
        private BinaryStream _stream = new BinaryStream(new FileStream(filename, FileMode.OpenOrCreate));
        private readonly int amount = chunks.width * chunks.height;
        private T[]?[] _buffer = new T[chunks.width * chunks.height][];
        protected readonly HashSet<(int chunkx, int chunky)> _updatedChunks = [];
        /// <summary>
        /// <see cref='this[int, int]'/> writes and reades Cells by original pos in world
        /// </summary>
        public T? this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= chunks.width * chunksize || y < 0 || y >= chunks.height * chunksize) return null;
                var pos = GetChunkPos(x, y);
                return Read(pos.x, pos.y)[GetCellIndex(x, y)];
            }
            set
            {
                if (x < 0 || x >= chunks.width * chunksize || y < 0 || y >= chunks.height * chunksize) return;
                var pos = GetChunkPos(x, y);
                var buffer = Read(pos.x, pos.y);
                buffer[GetCellIndex(x, y)] = value!.Value;
                _updatedChunks.Add(pos);
            }
        }
        /// <summary>
        /// <see cref='Unload'/> Unloads chunks from memory by chunk pos
        /// </summary>
        public void Unload(int chunkx, int chunky) => Unload(GetChunkIndex(chunkx, chunky));
        public void Unload(int chunkIndex)
        {
            if (chunkIndex < 0 || chunkIndex >= amount) return;
            _buffer[chunkIndex] = null;
        }
        /// <summary>
        /// <see cref='Commit'/> Commits World into file
        /// </summary>
        public void Commit()
        {
            var localch = new HashSet<(int chunkx, int chunky)>(_updatedChunks);
            _updatedChunks.Clear();
            foreach (var pos in localch)
            {
                var chunkindex = GetChunkIndex(pos.chunkx, pos.chunky);
                if (_buffer[chunkindex] is not null)
                {
                    Write(pos.chunkx, pos.chunky, _buffer[chunkindex]!);
                }
            }
        }
        
        private T[] Read(int index)
        {
            lock (_stream)
            {
                var chunk = new T[Count];
                Span<byte> temp = stackalloc byte[Count * typesize];
                _stream.Position = index * Count * typesize;
                _stream.Read(temp);
                for (int i = 0, j = 0; i < temp.Length; i += typesize, j++)
                    chunk[j] = MemoryMarshal.Read<T>(temp[i..(i + typesize)]);
                return chunk;
            }
        }
        private void Write(int index, T[] data)
        {
            lock (_stream)
            {
                    Span<byte> temp = stackalloc byte[data.Length * typesize];
                    for (int i = 0, j = 0; i < temp.Length; i += typesize, j++)
                        MemoryMarshal.Write(temp[i..(i + typesize)], in data[j]);
                    _stream.Position = index * Count * typesize;
                    _stream.Write(temp);
            }
        }
        public T[] Read(int chunkx, int chunky)
        {
            var chunkindex = GetChunkIndex(chunkx, chunky);
            _buffer[chunkindex] ??= Read(chunkindex);
            return _buffer[chunkindex];
        }
        public void Write(int chunkx, int chunky, T[] data) => Write(GetChunkIndex(chunkx, chunky), data);
        private (int x, int y) GetChunkPos(int x, int y) => ((int)Math.Floor((float)x / 32), (int)Math.Floor((float)y / 32));
        private int GetChunkIndex(int chunkx, int chunky) => chunky + chunks.height * chunkx;
        private int GetCellIndex(int x, int y)
        {
            var chpos = GetChunkPos(x, y);
            return y - chpos.y * 32 + chunksize * (x - chpos.x * 32);
        }
    }
}
