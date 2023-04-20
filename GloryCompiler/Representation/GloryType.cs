using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloryCompiler.Syntax;

namespace GloryCompiler.Representation
{
    public class GloryType
    {

        public GloryTypes Type;

        public GloryType(TokenType type)
        {
            Type = type switch
            {
                TokenType.BoolType => GloryTypes.Bool,
                TokenType.IntType => GloryTypes.Int,
                TokenType.StringType => GloryTypes.String,
                _ => throw new Exception("Wtf")
            };
        }
        public GloryType(GloryTypes type) => Type = type;

        public static bool operator ==(GloryType left, GloryType right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;

            if (left.Type != right.Type) return false;
            if (left is ArrayGloryType arr)
            {
                ArrayGloryType rightArr = arr;
                return arr.ItemType == rightArr.ItemType;
            }

            return true;
        }

        public static bool operator !=(GloryType left, GloryType right)
        {
            return !(left == right);
        }
    }

    internal class ArrayGloryType : GloryType
    {
        public GloryType ItemType;
        public int _size;
        public ArrayGloryType(GloryType itemType, int size) : base(GloryTypes.Array)
        {
            ItemType = itemType;
            _size = size;
        }
    }

    internal class ListGloryType : GloryType
    {
        public GloryType ItemType;
        public ListGloryType(GloryType itemType) : base(GloryTypes.List)
        {
            ItemType = itemType;
        }
    }

    public enum GloryTypes
    {
        Int,
        String,
        Bool,
        Array,
        List
    }
}
