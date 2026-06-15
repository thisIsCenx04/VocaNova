// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'quiz_result_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(quizResult)
const quizResultProvider = QuizResultFamily._();

final class QuizResultProvider
    extends
        $FunctionalProvider<
          AsyncValue<QuizResult>,
          QuizResult,
          FutureOr<QuizResult>
        >
    with $FutureModifier<QuizResult>, $FutureProvider<QuizResult> {
  const QuizResultProvider._({
    required QuizResultFamily super.from,
    required int super.argument,
  }) : super(
         retry: null,
         name: r'quizResultProvider',
         isAutoDispose: true,
         dependencies: null,
         $allTransitiveDependencies: null,
       );

  @override
  String debugGetCreateSourceHash() => _$quizResultHash();

  @override
  String toString() {
    return r'quizResultProvider'
        ''
        '($argument)';
  }

  @$internal
  @override
  $FutureProviderElement<QuizResult> $createElement($ProviderPointer pointer) =>
      $FutureProviderElement(pointer);

  @override
  FutureOr<QuizResult> create(Ref ref) {
    final argument = this.argument as int;
    return quizResult(ref, argument);
  }

  @override
  bool operator ==(Object other) {
    return other is QuizResultProvider && other.argument == argument;
  }

  @override
  int get hashCode {
    return argument.hashCode;
  }
}

String _$quizResultHash() => r'e982174636924ddbf295da0c94c736a41cb50d40';

final class QuizResultFamily extends $Family
    with $FunctionalFamilyOverride<FutureOr<QuizResult>, int> {
  const QuizResultFamily._()
    : super(
        retry: null,
        name: r'quizResultProvider',
        dependencies: null,
        $allTransitiveDependencies: null,
        isAutoDispose: true,
      );

  QuizResultProvider call(int sessionId) =>
      QuizResultProvider._(argument: sessionId, from: this);

  @override
  String toString() => r'quizResultProvider';
}
