# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2024-01-XX

### Added
- **Multiple Conversion Method Support**: Added support for `Parse`, `Create`, and `Convert` methods in addition to `FromString`
- **Method Priority System**: Intelligent selection of conversion methods with priority order: FromString > Parse > Create > Convert
- **Enhanced Type Detection**: Improved algorithm to find the most appropriate conversion method for each type
- **Comprehensive Test Suite**: Added extensive unit tests covering edge cases, error scenarios, and integration tests
- **Advanced Error Handling**: Better exception handling and propagation from conversion methods
- **Performance Optimizations**: Improved reflection caching and method lookup performance

### Changed
- **BREAKING**: Renamed library from `ToStringJsonConverter` to `RoundTripStringJsonConverter`
- **BREAKING**: Renamed main class from `ToStringJsonConverterFactory` to `RoundTripStringJsonConverterFactory`
- **BREAKING**: Changed namespace from `ktsu.ToStringJsonConverter` to `ktsu.RoundTripStringJsonConverter`
- Updated all documentation to reflect new name and enhanced functionality
- Improved method signature validation for better type safety

### Fixed
- Enhanced null argument validation
- Better handling of types with invalid method signatures
- Improved error messages for debugging

### Migration Guide
- Update package reference to `ktsu.RoundTripStringJsonConverter`
- Update using statements: `using ktsu.RoundTripStringJsonConverter;`
- Update converter instantiation: `new RoundTripStringJsonConverterFactory()`
- Existing `FromString` methods continue to work unchanged
- You can now also use `Parse`, `Create`, or `Convert` methods

## [1.2.4] - Previous Release

Initial release of repository with no prior history.

