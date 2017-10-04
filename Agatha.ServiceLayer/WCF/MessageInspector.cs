using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using Agatha.ServiceLayer.Logging;

namespace Agatha.ServiceLayer.WCF
{
    public class MessageInspector : IDispatchMessageInspector
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(MessageInspector));
        private readonly ILog _messageLogger = LogProvider.GetLogger("WCF.Messages");

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            if (_logger.IsInfoEnabled()) {
                var bufferedCopy = request.CreateBufferedCopy(int.MaxValue);

                LogMessage("request", bufferedCopy.CreateMessage());

                request = bufferedCopy.CreateMessage();
            }

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if (_logger.IsInfoEnabled() && reply != null)
            {
                var bufferedCopy = reply.CreateBufferedCopy(int.MaxValue);

                LogMessage("response", bufferedCopy.CreateMessage());

                reply = bufferedCopy.CreateMessage();
            }
        }

        private void LogMessage(string messageType, Message message)
        {
            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = false };

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = XmlDictionaryWriter.Create(memoryStream, writerSettings))
                {
                    message.WriteMessage(writer);
                    writer.Flush();
                    var size = Math.Round(memoryStream.Position/1024d, 2);
                    _logger.InfoFormat("{0} message size: ~{1} KB", messageType, size);
                }

                if (_messageLogger.IsDebugEnabled())
                {
                    memoryStream.Position = 0;
                    using (var reader = new StreamReader(memoryStream))
                    {
                        _messageLogger.Debug(reader.ReadToEnd());
                    }
                }
            }

        }
    }
}