using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClient
{
    internal class ChatClient
    {
        private ConnectionHandler.ConnectionHandler _connectionHandler;
        private Logger.Logger _logger;
        private CLI.CLI _cli;

        public ChatClient(CLI.CLI cli, Logger.Logger logger)
        {
            _connectionHandler = new ConnectionHandler.ConnectionHandler();
            _logger = logger;
            _cli = cli;
        }

        public void Start(IPEndPoint endPoint)
        {
            _logger.LogInfo($"Connecting to: {endPoint.Address}...");
            try
            {
                _connectionHandler.StartAsClient(endPoint);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                Stop();
                return;
            }
            finally
            {
                _logger.LogSuccess("Connection established succesfully!");

                Thread connectionThread = new Thread(_handleConnection);
                connectionThread.Start();
            }
        }

        public void Stop()
        {
            _connectionHandler.Stop();
        }

        private void _handleConnection()
        {

        }
    }
}
