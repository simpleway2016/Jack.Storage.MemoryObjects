using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    class StorageContextEnumerator<T> : IEnumerator<T>
    {
        List<DataItem<T>> _source;
        int _position = 0;
        public StorageContextEnumerator(List<DataItem<T>> source)
        {
            _source = source;
            this.Reset();
        }

        public T Current
        {
            get
            {
                if (_position < _source.Count)
                {
                    return _source[_position].Data;
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
            _position++;
            while (_position < _source.Count && _source[_position] == null)
            {
                _position++;
            }


            return _position < _source.Count;
        }

        public void Reset()
        {
            _position = 0;
            while (_position < _source.Count && _source[_position] == null)
            {
                _position++;
            }

        }
    }
}
