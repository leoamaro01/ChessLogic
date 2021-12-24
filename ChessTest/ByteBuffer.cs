using System;
using System.Collections.Generic;
using System.Text;

public class ByteBuffer : IDisposable
{
    private List<byte> buffer;
    private byte[] readBuffer;
    private int readPos;
    private bool bufferUpdated = false;

    #region Functions
    public ByteBuffer()
    {
        readBuffer = new byte[] { };
        buffer = new List<byte>(); // Intitialize buffer
        readPos = 0; // Set readPos to 0
    }

    public int GetReadPos()
    {
        return readPos; // Return the position where we are reading from in the byte array
    }

    public byte[] ToArray()
    {
        return buffer.ToArray(); // Return a byte array of buffer
    }

    public int Count()
    {
        return buffer.Count; // Return the length of buffer
    }

    public int Length()
    {
        return Count() - readPos; // Return the remaining length (unread)
    }

    public void Clear()
    {
        buffer.Clear(); // Clear buffer
        readPos = 0; // Reset readPos
    }
    #endregion

    #region"Read & Write Data"
    #region "Byte"
    /// <summary>Writes a byte to the buffer.</summary>
    public void WriteByte(byte _input)
    {
        buffer.Add(_input); // Add _input to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a byte from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public byte ReadByte(bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            byte _value = readBuffer[readPos]; // Get the byte at readPos' position
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                readPos += 1; // Increase readPos by 1
            }
            return _value; // Return the byte
        }
        else
        {
            throw new Exception("Could read value of type 'byte'!");
        }
    }
    #endregion

    #region "Bytes"
    /// <summary>Writes a byte array to the buffer.</summary>
    public void WriteBytes(byte[] _input)
    {
        buffer.AddRange(_input); // Add _input to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a byte array from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public byte[] ReadBytes(int _length, bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            byte[] _value = buffer.GetRange(readPos, _length).ToArray(); // Get the bytes at readPos' position with a range of _length
            if (_peek)
            {
                // If _peek is true
                readPos += _length; // Increase readPos by _length
            }
            return _value; // Return the bytes
        }
        else
        {
            throw new Exception("Could read value of type 'byte[]'!");
        }
    }
    #endregion

    #region "Short"
    /// <summary>Writes a short to the buffer.</summary>
    public void WriteShort(short _input)
    {
        buffer.AddRange(BitConverter.GetBytes(_input)); // Convert short to bytes and add them to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a short from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public short ReadShort(bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            short _value = BitConverter.ToInt16(readBuffer, readPos); // Convert the bytes to a short
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                readPos += 2; // Increase readPos by 2
            }
            return _value; // Return the short
        }
        else
        {
            throw new Exception("Could read value of type 'short'!");
        }
    }
    #endregion

    #region "Int"
    /// <summary>Writes an int to the buffer.</summary>
    public void WriteInt(int _input)
    {
        buffer.AddRange(BitConverter.GetBytes(_input)); // Convert int to bytes and add them to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads an int from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public int ReadInt(bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            int _value = BitConverter.ToInt32(readBuffer, readPos); // Convert the bytes to an int
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                readPos += 4; // Increase readPos by 4
            }
            return _value; // Return the int
        }
        else
        {
            throw new Exception("Could read value of type 'int'!");
        }
    }
    #endregion

    #region "Long"
    /// <summary>Writes a long to the buffer.</summary>
    public void WriteLong(long _input)
    {
        buffer.AddRange(BitConverter.GetBytes(_input)); // Convert long to bytes and add them to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a long from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public long ReadLong(bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            long _value = BitConverter.ToInt64(readBuffer, readPos); // Convert the bytes to a long
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                readPos += 8; // Increase readPos by 8
            }
            return _value; // Return the long
        }
        else
        {
            throw new Exception("Could read value of type 'long'!");
        }
    }
    #endregion

    #region "Float"
    /// <summary>Writes a float to the buffer.</summary>
    public void WriteFloat(float _input)
    {
        buffer.AddRange(BitConverter.GetBytes(_input)); // Convert float to bytes and add them to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a float from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public float ReadFloat(bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            float _value = BitConverter.ToSingle(readBuffer, readPos); // Convert the bytes to a float
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                readPos += 4; // Increase readPos by 4
            }
            return _value; // Return the float
        }
        else
        {
            throw new Exception("Could read value of type 'float'!");
        }
    }
    #endregion

    #region "String"
    /// <summary>Writes a string to the buffer.</summary>
    public void WriteString(string _input)
    {
        buffer.AddRange(BitConverter.GetBytes(_input.Length)); // Convert the length of the string (_input.Length) to bytes and add them to buffer
        buffer.AddRange(Encoding.ASCII.GetBytes(_input)); // Convert string to bytes and add them to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a string from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public string ReadString(bool _peek = true)
    {
        try
        {
            int _length = ReadInt(true); // Get the length of the string
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            string _value = Encoding.ASCII.GetString(readBuffer, readPos, _length); // Convert the bytes to a string
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                if (_value.Length > 0)
                {
                    // If the string length is > 0
                    readPos += _length; // Increase readPos by the length of the string
                }
            }
            return _value; // Return the string
        }
        catch
        {
            throw new Exception("Could read value of type 'short'!");
        }
    }
    #endregion

    #region "Bool"
    /// <summary>Writes a bool to the buffer.</summary>
    public void WriteBool(bool _input)
    {
        buffer.AddRange(BitConverter.GetBytes(_input)); // Convert bool to bytes and add them to buffer
        bufferUpdated = true;
    }

    /// <summary>Reads a bool from the buffer</summary>
    /// <param name="_peek">Move the buffer's read position.</param>
    public bool ReadBool(bool _peek = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            bool _value = BitConverter.ToBoolean(readBuffer, readPos); // Convert the bytes to a bool
            if (_peek & buffer.Count > readPos)
            {
                // If _peek is true and there are unread bytes
                readPos += 1; // Increase readPos by 1
            }
            return _value; // Return the bool
        }
        else
        {
            throw new Exception("Could read value of type 'bool'!");
        }
    }
    #endregion
    #endregion

    private bool disposedValue = false;

    protected virtual void Dispose(bool _disposing)
    {
        if (!disposedValue)
        {
            if (_disposing)
            {
                buffer.Clear();
                readPos = 0;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}