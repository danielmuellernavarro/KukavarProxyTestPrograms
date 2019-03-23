using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using System.Text;

namespace KVP_TCPClient
{
    class Kukavarproxy
    {
        public class Result
        {
            public bool Succeeded { get; set; }
            public String Data { get; set; }
            public string ErrorMessage { get; set; }

            public Result()
            {
                Succeeded = false;
                Data = "";
                ErrorMessage = "";
            }
        }

        class ByteBuilder
        {
            public List<byte[]> _bytes;
            public ByteBuilder()
            {
                _bytes = new List<byte[]>();
            }
            public void AppendBytes(byte[] arrayBytes)
            {
                _bytes.Add(arrayBytes);
            }
        }

        private int _msgID;
        private readonly int _firstID, _lastID;
        private readonly uint _readBuffer = 4096;
        private StreamSocket _socket;
        public Kukavarproxy(int firstID, int lastID)
        {
            _firstID = firstID;
            _lastID = lastID;
            _msgID = firstID;
        }

        public async Task<Result> Connect(string Port, string IP)
        {
            Result result = new Result();
            _socket = new StreamSocket();
            GC.Collect(); // Clean garbage collection
            try
            {
                await _socket.ConnectAsync(new HostName(IP), Port);
                result.Succeeded = true;
                return result;
            }
            catch (Exception ex)
            {
                SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
                result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                return result;
            }
        }

        public async Task<Result> ReadVariable(string pVarName)
        {
            Result result = new Result();

            // Read Variable request packet structure example:
            //   0  1     2  3        4       5  6   
            //  xx xx  | 00 0A   |   00    | 00 07        | 24 4F 56 5F 50 52 4F
            //         |   10    |    0    |     7        | $  O  V  _  P  R  O
            //  REQ ID | REQ LEN | READ=00 | VAR NAME LEN | VAR NAME CHARS
            // (RANDOM)|

            if (pVarName.Length == 0)
            {
                result.ErrorMessage = "No variable was written";
                return result;
            }

            var PKT_var_name = new byte[pVarName.Length];
            PKT_var_name = Encoding.UTF8.GetBytes(pVarName);

            var PKT_name_length = new byte[2];
            PKT_name_length[0] = (byte)((pVarName.Length >> 8) & 255);
            PKT_name_length[1] = (byte)(pVarName.Length & 255);

            byte PKT_mode_is_read = 0;

            var PKT_req_len = new byte[2];
            PKT_req_len[0] = (byte)(((pVarName.Length + 3) >> 8) & 255);
            PKT_req_len[1] = (byte)((pVarName.Length + 3) & 255);

            var PKT_req_id = new byte[2];
            PKT_req_id[0] = (byte)((_msgID >> 8) & 255);
            PKT_req_id[1] = (byte)(_msgID & 255);
            _msgID++;
            if (_msgID > _lastID) _msgID = _firstID;

            var REQ_packet = new byte[pVarName.Length + 7 + 1];
            REQ_packet[0] = PKT_req_id[0];
            REQ_packet[1] = PKT_req_id[1];
            REQ_packet[2] = PKT_req_len[0];
            REQ_packet[3] = PKT_req_len[1];
            REQ_packet[4] = PKT_mode_is_read;
            REQ_packet[5] = PKT_name_length[0];
            REQ_packet[6] = PKT_name_length[1];
            PKT_var_name.CopyTo(REQ_packet, 7);

            DataWriter writer;

            using (writer = new DataWriter(_socket.OutputStream))
            {
                // Set the Unicode character encoding for the output stream
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                // Specify the byte order of a stream.
                writer.ByteOrder = ByteOrder.LittleEndian;

                // Gets the size of UTF-8 string.
                writer.MeasureString(Encoding.UTF8.GetString(REQ_packet));
                // Write a string value to the output stream.
                writer.WriteBytes(REQ_packet);

                // Send the contents of the writer to the backing stream.
                try
                {
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                    result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                    return result;
                }
                // In order to prolong the lifetime of the stream, detach it from the DataWriter
                writer.DetachStream();
            }

            // Read Variable response packet structure example:
            // 0  1     2  3      4         5  6          
            // xx xx  | 00 0A   | 00      | 00 06       | 35 35 33 39 39 33 | 00 01 01
            //        |   10    |  0      |     6       | 5  5  3  9  9  3  |  0  1  1
            // SAME AS| RSP LEN | READ=00 | VALUE LEN   | VALUE CHARS       |  TRAILER
            // REQUEST|

            DataReader reader;
            ByteBuilder byteBuilder = new ByteBuilder();

            using (reader = new DataReader(_socket.InputStream))
            {
                // Read the length of the payload that will be received.
                reader.InputStreamOptions = InputStreamOptions.Partial;
                // The encoding and byte order need to match the settings of the writer we previously used.
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;
                // Send the contents of the writer to the backing stream. 
                // Get the size of the buffer that has not been read.
                try
                {
                    do
                    {
                        await reader.LoadAsync(_readBuffer);
                        var bytes = new Byte[reader.UnconsumedBufferLength];
                        reader.ReadBytes(bytes);
                        byteBuilder.AppendBytes(bytes);
                        // Keep reading until we consume the complete stream.
                    }
                    while (reader.UnconsumedBufferLength > 0);
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
                    result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                    return result;
                }
                finally
                {
                    reader.DetachStream();
                }
            }

            var flattenedList = byteBuilder._bytes.SelectMany(bytes => bytes);
            var RSP_packet = flattenedList.ToArray();

            try
            {
                int RSP_len = (RSP_packet[2] << 8) | RSP_packet[3];
                int RSP_val_len = (RSP_packet[5] << 8) | RSP_packet[6];
                result.Data = Encoding.ASCII.GetString(RSP_packet, 7, RSP_val_len);
                int RSP_read_status = RSP_packet[7 + (RSP_val_len + 2)];
                if ((RSP_read_status > 0) & (RSP_val_len > 0) &
                    (RSP_packet[0] == PKT_req_id[0]) & (RSP_packet[1] == PKT_req_id[1]))
                {
                    result.Succeeded = true;
                    return result;
                }
                else
                {
                    result.ErrorMessage = "Cannot be read";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }

        }

        public async Task<Result> WriteVariable(string pVarName, string pValue)
        {
            Result result = new Result();

            // Write Variable request packet structure example:
            //   0  1     2  3      4         5  6  
            //  xx xx  | 00 0F   | 01       | 00 07        | 24 4F 56 5F 50 52 4F | 00 03   |     31 32 33
            //         |   15    |  1       |     7        | $  O  V  _  P  R  O  |     3   |      1  2  3
            //  REQ ID | REQ LEN | WRITE=1  | VAR NAME LEN | VAR NAME CHARS       | VAL LEN | VAL AS STRING
            // (RANDOM)|

            if ((pVarName.Length == 0) || (pValue.Length == 0))
            {
                result.ErrorMessage = "No variable or value was written";
                return result;
            }

            var PKT_value = new byte[pValue.Length];
            PKT_value = Encoding.UTF8.GetBytes(pValue);

            var PKT_value_len = new byte[2];
            PKT_value_len[0] = (byte)((pValue.Length >> 8) & 255);
            PKT_value_len[1] = (byte)(pValue.Length & 255);

            var PKT_var_name = new byte[pVarName.Length];
            PKT_var_name = Encoding.UTF8.GetBytes(pVarName);

            var PKT_name_length = new byte[2];
            PKT_name_length[0] = (byte)((pVarName.Length >> 8) & 255);
            PKT_name_length[1] = (byte)(pVarName.Length & 255);

            byte PKT_mode_is_write = 1;

            var PKT_req_id = new byte[2];
            PKT_req_id[0] = (byte)((_msgID >> 8) & 255);
            PKT_req_id[1] = (byte)(_msgID & 255);

            _msgID++;
            if (_msgID > _lastID) _msgID = _firstID; // ID from 1 to 1000

            var PKT_req_len = new byte[2];
            PKT_req_len[0] = (byte)(((5 + pVarName.Length + pValue.Length) >> 8) & 255);
            PKT_req_len[1] = (byte)((5 + pVarName.Length + pValue.Length) & 255);

            var REQ_packet = new byte[5 + 2 + pVarName.Length + 2 + pValue.Length];
            REQ_packet[0] = PKT_req_id[0];
            REQ_packet[1] = PKT_req_id[1];
            REQ_packet[2] = PKT_req_len[0];
            REQ_packet[3] = PKT_req_len[1];
            REQ_packet[4] = PKT_mode_is_write;
            REQ_packet[5] = PKT_name_length[0];
            REQ_packet[6] = PKT_name_length[1];
            PKT_var_name.CopyTo(REQ_packet, 7);
            PKT_value_len.CopyTo(REQ_packet, 7 + pVarName.Length);
            PKT_value.CopyTo(REQ_packet, 7 + pVarName.Length + PKT_value_len.Length);

            DataWriter writer;

            using (writer = new DataWriter(_socket.OutputStream))
            {
                // Set the Unicode character encoding for the output stream
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                // Specify the byte order of a stream.
                writer.ByteOrder = ByteOrder.LittleEndian;

                // Gets the size of UTF-8 string.
                writer.MeasureString(Encoding.UTF8.GetString(REQ_packet));
                // Write a string value to the output stream.
                writer.WriteBytes(REQ_packet);

                // Send the contents of the writer to the backing stream.
                try
                {
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                    result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                    return result;
                }
                // In order to prolong the lifetime of the stream, detach it from the DataWriter
                writer.DetachStream();
            }

            // Write Variable response packet structure example:
            //   0  1     2  3         4      5  6  
            //  xx xx  | 00 0A   |    01   | 00 06       | 35 35 33 39 39 33   | 00  | 01 01
            //         |    10   |     1   |     6       |  5  5  3  9  9  3   |  0  |  1  1
            // SAME AS | RSP LEN | WRITE=1 | VALUE LEN   | WRITTEN VALUE CHARS | PAD | READ status 01 01 = OK
            // REQUEST |

            DataReader reader;
            ByteBuilder byteBuilder = new ByteBuilder();

            using (reader = new DataReader(_socket.InputStream))
            {
                // Read the length of the payload that will be received.
                reader.InputStreamOptions = InputStreamOptions.Partial;
                // The encoding and byte order need to match the settings of the writer we previously used.
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;

                // Send the contents of the writer to the backing stream. 
                // Get the size of the buffer that has not been read.
                try
                {
                    do
                    {
                        await reader.LoadAsync(_readBuffer);
                        var bytes = new Byte[reader.UnconsumedBufferLength];
                        reader.ReadBytes(bytes);
                        byteBuilder.AppendBytes(bytes);
                        // Keep reading until we consume the complete stream.
                    }
                    while (reader.UnconsumedBufferLength > 0);
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
                    result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                    return result;
                }
                finally
                {
                    reader.DetachStream();
                }
            }

            var flattenedList = byteBuilder._bytes.SelectMany(bytes => bytes);
            var RSP_packet = flattenedList.ToArray();

            try
            {
                int RSP_val_len = (((RSP_packet[5] << 8) & 255) + RSP_packet[6]);
                result.Data = Encoding.ASCII.GetString(RSP_packet, 7, RSP_val_len);
                int RSP_read_status = RSP_packet[7 + (RSP_val_len + 2)];
                if ((RSP_read_status > 0) &
                    (RSP_packet[0] == PKT_req_id[0]) & (RSP_packet[1] == PKT_req_id[1]))
                {
                    result.Succeeded = true;
                    return result;
                }
                else
                {
                    result.ErrorMessage = "Result cannot be read";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<Result> Write(byte[] data, Result resultInput)
        {
            Result result = resultInput;
            DataWriter writer;

            using (writer = new DataWriter(_socket.OutputStream))
            {
                // Set the Unicode character encoding for the output stream
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                // Specify the byte order of a stream.
                writer.ByteOrder = ByteOrder.LittleEndian;

                // Gets the size of UTF-8 string.
                writer.MeasureString(Encoding.UTF8.GetString(data));
                // Write a string value to the output stream.
                writer.WriteBytes(data);

                // Send the contents of the writer to the backing stream.
                try
                {
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                    result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                    return result;
                }
                // In order to prolong the lifetime of the stream, detach it from the DataWriter
                writer.DetachStream();
            }
            return result;
        }

        private async Task<Tuple<Result, byte[]>> Read(Result resultInput)
        {
            Result result = resultInput;
            DataReader reader;
            StringBuilder strBuilder;

            using (reader = new DataReader(_socket.InputStream))
            {
                strBuilder = new StringBuilder();

                // Set the DataReader to only wait for available data (so that we don't have to know the data size)
                reader.InputStreamOptions = InputStreamOptions.Partial;
                // The encoding and byte order need to match the settings of the writer we previously used.
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;

                // Send the contents of the writer to the backing stream. 
                // Get the size of the buffer that has not been read.
                try
                {
                    do
                    {
                        await reader.LoadAsync(_readBuffer);
                        strBuilder.Append(reader.ReadString(reader.UnconsumedBufferLength));
                        // Keep reading until we consume the complete stream.
                    }
                    while (reader.UnconsumedBufferLength > 0);
                }
                catch (Exception ex)
                {
                    SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
                    result.ErrorMessage = (webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                    var RSP_packet2 = new byte[strBuilder.ToString().Length];
                    return new Tuple<Result, byte[]>(result, RSP_packet2);
                }
                finally
                {
                    reader.DetachStream();
                }
            }
            var RSP_packet = new byte[strBuilder.ToString().Length];
            RSP_packet = Encoding.UTF8.GetBytes(strBuilder.ToString());
            return new Tuple<Result, byte[]>(result, RSP_packet);
        }
    }
}
