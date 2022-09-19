## GeneratedIndexOfAnyAttribute

Prototype for [Vectorize IndexOfAny on more than 5 chars](https://github.com/dotnet/runtime/issues/68328).

Based on ideas from [Optimize FindFirstCharToEncode for JavaScriptEncoder.Default using Ssse3 intrinsics](https://github.com/dotnet/corefx/pull/42073), which is based on ideas from [Base64 encoding with simd-support](https://github.com/dotnet/corefx/pull/34529/files#diff-602ae9361214acd45d8749141bd1f0f49238e9e362d718d32dff864697d20c80R527-R533) ([these lines](https://github.com/dotnet/corefx/blob/cf4f5ce7ba3792f63967d5fe17f28ada84065129/src/System.Memory/src/System/Buffers/Text/Base64Decoder.cs#L525-L533)) which itself is based on [Base64 decoding with SIMD instructions](http://0x80.pl/notesen/2016-01-17-sse-base64-decoding.html) from [Wojciech Muła](https://github.com/WojciechMula) and some other post from him.

The origin impulse for this approach was in [Kestrel response header encoding
](https://github.com/dotnet/aspnetcore/pull/33776#discussion_r656939393) where I did a quick prototype (which is in _master_-branch of this repo).
After the PR [Vectorized HttpCharacters](https://github.com/dotnet/aspnetcore/pull/44041) got created [Stephen Toub](https://github.com/stephentoub) brought my attention the issue mentioned above. So this prototype got created.

---

Rough description on how the bitmask-approach works is given in [this comment](https://github.com/dotnet/corefx/pull/41845#discussion_r336745516).
