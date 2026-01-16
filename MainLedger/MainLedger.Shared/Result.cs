using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainLedger.Shared
{
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string? Error { get; protected set; }

        protected Result(bool isSucess)
        {
            IsSuccess = isSucess;
        }

        protected Result(string error)
        {
            Error = error;
        }


        public static Result Success()
        {
            return new Result(true);
        }

        public static Result Failure(string error)
        {
            return new Result(error);
        }
    }
    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        private Result(T? value, bool isSucess) : base(isSucess)
        {
            Value = value;
        }

        private Result(string error) : base(error)
        {
        }


        public static Result<T> Success(T? value)
        {
            return new Result<T>(value, true);
        }

        public static new Result<T> Failure(string error)
        {
            return new(error);
        }
    }
}
