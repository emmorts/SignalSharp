set shell := ["nu", "-c"]

set dotenv-load := true

default_env := "Development"

default:
    @just --list
    
format:
    dotnet csharpier format .

benchmark-list:
    @dotnet run --project SignalSharp.Benchmark\SignalSharp.Benchmark.csproj -c Release -- --job short --runtimes net9.0 --list flat

benchmark-run filter:
    @dotnet run --project SignalSharp.Benchmark\SignalSharp.Benchmark.csproj -c Release -- --job short --filter {{filter}}

benchmark-run-all:
    @dotnet run --project SignalSharp.Benchmark\SignalSharp.Benchmark.csproj -c Release -- --job short -f '*'
