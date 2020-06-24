using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    class StorageContextEnumerator<T> : IEnumerator<T>
    {
        List<T> _source;
        int _position = 0;
        public StorageContextEnumerator(List<T> source)
        {
            _source = source;
        }

        public T Current
        {
            get
            {
                if (_position < _source.Count)
                {
                    try
                    {
                        return _source[_position];
                    }
                    catch
                    {
                        return default(T);
                    }
                }

                return default(T);
            }
        }

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            if (_position < _source.Count - 1)
            {
                _position++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _position = 0;
        }
    }
}
