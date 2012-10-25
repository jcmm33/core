using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vici.Core.Parser
{
    /// <summary>
    /// Match a token against the pattern for a generic variable
    /// </summary>
    /// <remarks>A generic variable is of the form:
    /// variablename<type1,type2,type3>
    /// </remarks>
    public class GenericVariableMatcher : ITokenMatcher, ITokenProcessor
    {
        /// <summary>
        /// The characters considered valid for the start of a variable name
        /// </summary>
        private const string VALID_FIRSTCHARS = "abcdefghijkklmnopqrstuvwxyzABCDEFGHIJKKLMNOPQRSTUVWXYZ_@$";

        /// <summary>
        /// The characters considered valid for non first variable names
        /// </summary>
        private const string VALID_NEXTCHARS = "abcdefghijkklmnopqrstuvwxyzABCDEFGHIJKKLMNOPQRSTUVWXYZ_@$0123456789";

        /// <summary>
        /// The characters considered valie for a generic placeholder (and separator)
        /// </summary>
        private const string VALID_GENERICCHARS = "abcdefghijkklmnopqrstuvwxyzABCDEFGHIJKKLMNOPQRSTUVWXYZ_@$0123456789,?";

        /// <summary>
        /// Have we seen a starting character ?
        /// </summary>
        private bool _sawFirst;

        /// <summary>
        /// Have we seen the open < for the generic
        /// </summary>
        private bool _sawOpen;

        /// <summary>
        /// Have we seen the closing > for the generic
        /// </summary>
        private bool _sawClose;

        public void ResetState()
        {
            _sawFirst = false;
            _sawOpen = false;
            _sawClose = false;

        }

        ITokenProcessor ITokenMatcher.CreateTokenProcessor()
        {
            return new GenericVariableMatcher();
        }

        TokenizerState ITokenProcessor.ProcessChar(char c, string fullExpression, int currentIndex)
        {
            // if we have seen the close of the generic then we must be done
            if (_sawClose)
            {
                return TokenizerState.Success;
            }

            // if we having seen anything to kick things off then flag this 
            if (!_sawFirst)
            {
                _sawFirst = true;

                return VALID_FIRSTCHARS.IndexOf(c) >= 0 ? TokenizerState.Valid : TokenizerState.Fail;
            }

            // if we haven't started on the generic place holders
            if (!_sawOpen)
            {
                // do we have the character indicating generic placeholders will follow 
                if (c == '<')
                {
                    _sawOpen = true;

                    return TokenizerState.Valid;
                }

                // is this a valid character for a type placeholder
                return VALID_NEXTCHARS.IndexOf(c) >= 0 ? TokenizerState.Valid : TokenizerState.Success;
            }


            // got this far, must be in the generic part...
            if (c == '>')
            {
                _sawClose = true;

                return TokenizerState.Valid;
            }

            return VALID_GENERICCHARS.IndexOf(c) >= 0 ? TokenizerState.Valid : TokenizerState.Fail;
        }

        public string TranslateToken(string originalToken, ITokenProcessor tokenProcessor)
        {
            return originalToken;
        }
    }
}
