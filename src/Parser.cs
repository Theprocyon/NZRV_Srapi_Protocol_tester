using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Parser
    {
        public const short STX = 0x02;
        public const short GS = 0x1D;
        public const short US = 0x1F;
        public const short ETX = 0x03;

        private const int PARSER_STRING_LENGTH_MAX = 120;
        private enum State
        {
            PARSE_STATE_IDLE,
            PARSE_STATE_GOT_STX,
            PARSE_STATE_GOT_API,
            PARSE_STATE_GOT_CHKSUM
        }

        State mParserState = State.PARSE_STATE_IDLE;

        int mCount;
        byte mChecksum;

        StringBuilder mStringBuilder;
        String mApiName;

        public Parser() { Initialize(); }
        public void Initialize()
        {
            mApiName = String.Empty;

            if (mStringBuilder == null)
                mStringBuilder = new StringBuilder();
            mStringBuilder.Clear();

            mCount = 0;
            mParserState = State.PARSE_STATE_IDLE;
            mChecksum = 0;
        }
        public String parseChar(byte _c)
        {
            switch (this.mParserState)
            {
                case State.PARSE_STATE_IDLE:
                    if (_c == STX)
                    {
                        mParserState = State.PARSE_STATE_GOT_STX;
                    }
                    mCount = 0;
                    break;

                case State.PARSE_STATE_GOT_STX:
                    mChecksum ^= _c;
                    mCount++;
                    if (_c == GS)
                    {
                        mApiName = mStringBuilder.ToString();
                        mStringBuilder.Clear();
                        mParserState = State.PARSE_STATE_GOT_API;
                        mCount = 0;
                        
                        break;
                    }
                    if (mCount > PARSER_STRING_LENGTH_MAX)
                    {
                        parseFail("API length exceeds " + PARSER_STRING_LENGTH_MAX);
                        break;
                    }
                    mStringBuilder.Append((char)_c);
                    break;

                case State.PARSE_STATE_GOT_API:
                    mChecksum |= 0x20;
                    Console.WriteLine("Checksum is " + (int)_c);
                    if (mChecksum != _c)
                    {
                        parseFail("App Checksum : " + mChecksum + ", rcvd Checksum : " + _c);
                        break;
                    }
                    this.mParserState = State.PARSE_STATE_GOT_CHKSUM;
                    break;

                case State.PARSE_STATE_GOT_CHKSUM:
                    if (_c == ETX)
                    {
                        if (String.Equals(this.mApiName, "/api/module/v1/pmc/bat/status"))
                        {
                            return mApiName;
                        }
                        else
                        {
                            Console.WriteLine("Parse result different api!!");
                            return "";
                        }
                    }
                    break;
            }
            return "";
        }
        private void parseFail(String _msg)
        {
            Console.WriteLine("Parse failed at State " + this.mParserState.ToString() + ", following reason. " + _msg);
            Initialize();
        }
    }
}
