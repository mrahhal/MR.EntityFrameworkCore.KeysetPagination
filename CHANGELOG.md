# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## 1.5.0 - 2025-04-06

This version targets .net 8.

### Fixed

- Fix an analyzer issue ([#60](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/60) by [@mrahhal](https://github.com/mrahhal))

### Changed

- Update to .net 8 ([#62](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/62) by [@mrahhal](https://github.com/mrahhal))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.4.1...v1.5.0)

## 1.4.1 - 2024-02-02

### Fixed

- Add Enum support in keyset ([#54](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/54) by [@vpachta](https://github.com/vpachta))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.4.0...v1.4.1)

## 1.4.0 - 2024-01-28

### Added

- Make ConfigureColumn public ([#51](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/51) by [@dannyheard7](https://github.com/dannyheard7))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.3.0...v1.4.0)

## 1.3.0 - 2023-05-13

This release overhauls how expressions that build keyset columns are dealt with. A lot more patterns are now supported, but it's up to you as the consumer to make sure that the more complicated keysets are up to the performance standard you expect.

### Improved

- Expression adapting ([#37](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/37) by [@mrahhal](https://github.com/mrahhal))
- Unify expression accessing reference values ([#42](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/42) by [@mrahhal](https://github.com/mrahhal))

### Added

- Prebuilt keyset query definition ([#43](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/43) by [@mrahhal](https://github.com/mrahhal))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.2.0...v1.3.0)

## 1.2.0 - 2022-11-03

The highlight of this release is an analyzer that detects possible problems when configuring keyset columns (in particular, nullable columns are not supported).

### Improved

- Improve exception messages and catch more problems to expose clearer messages

### Other

- Add NULL caveat doc and add tests/samples/guiding around the computed workaround ([#25](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/25) by [@mrahhal](https://github.com/mrahhal))
- Add an analyzer that detects unsupported nullable properties in the keyset ([#26](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/26) by [@mrahhal](https://github.com/mrahhal))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.1.0...v1.2.0)

## 1.1.0 - 2022-10-24

### Added

- Support nested properties when defining a keyset ([#23](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/23) by [@mrahhal](https://github.com/mrahhal))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.0.3...v1.1.0)

## 1.0.3 - 2022-06-16

### Improved

- Generate expressions to use sql parameters instead of constants ([#19](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/pull/19))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.0.2...v1.0.3)

## 1.0.2 - 2022-05-14

### Added

- Support booleans ([#16](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/16))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.0.1...v1.0.2)

## 1.0.1 - 2022-02-18

### Fixed

- Fix problem with nullable props ([#14](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/14))

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.0.0...v1.0.1)

## 1.0.0 - 2021-12-01

### Improved

- Optimize the generated predicate expression to use an access predicate ([#8](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/8))
- Throw custom exception when data type is incompatible ([#11](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/11))

### Added

- Add EnsureCorrectOrder to KeysetContext

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v0.2.0...v1.0.0)

## 0.2.1 - 2021-11-24

### Fixed

- Fix order of expressions generated for the order by when acting on more than one column

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v0.2.0...v0.2.1)

## 0.2.0 - 2021-11-23

This version targets .net 6.

### Added

- Use NRTs (nullable references types)

### Changed

- Update to .net 6

[**Full diff**](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v0.1.3...v0.2.0)

## 0.1.3 - 2021-11-01

### Added

- Support Guids ([#1](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/1))
