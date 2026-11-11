## v1.0.77 (patch)

Changes since v1.0.76:

- Read and write IFormattable types with both Parse overloads in the invariant culture [patch] (#137) (matt-edmondson)
- Refuse to write a value whose ToString() returns null [patch] (#139) (matt-edmondson)
- Keep converting a type whose [JsonConverter] names this factory [patch] (#142) (matt-edmondson)
- Convert an abstract base or interface whose derived types override ToString [patch] (#143) (matt-edmondson)
- Pass the type's default for an [Optional] parameter with no declared default [patch] (#146) (matt-edmondson)
- Stop pinning the caller's culture when writing a non-IFormattable value [patch] (#148) (matt-edmondson)
- Leave a type with its own [JsonConverter] to that converter [patch] (#133) (matt-edmondson)
- Regenerate CompatibilitySuppressions.xml after the Polyfill 11.4.5 bump [patch] (#134) (matt-edmondson)
- Support dictionary keys in the netstandard2.0 and netstandard2.1 builds [patch] (#130) (matt-edmondson)
- Leave types that don't override ToString to System.Text.Json [patch] (#129) (matt-edmondson)
- Write non-IFormattable values with the invariant culture too [patch] (matt-edmondson)

