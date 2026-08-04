import 'package:flutter/material.dart';

abstract final class AppTextStyles {
  static const fontFamily = 'Inter';

  static const heading = TextStyle(
    fontFamily: fontFamily,
    fontSize: 26,
    height: 39 / 26,
    fontWeight: FontWeight.w700,
  );

  static const body = TextStyle(
    fontFamily: fontFamily,
    fontSize: 15,
    height: 22.5 / 15,
    fontWeight: FontWeight.w400,
  );

  static const caption = TextStyle(
    fontFamily: fontFamily,
    fontSize: 13,
    height: 19.5 / 13,
    fontWeight: FontWeight.w400,
  );

  static const label = TextStyle(
    fontFamily: fontFamily,
    fontSize: 13,
    height: 19.5 / 13,
    fontWeight: FontWeight.w500,
  );

  static const button = TextStyle(
    fontFamily: fontFamily,
    fontSize: 15,
    height: 22.5 / 15,
    fontWeight: FontWeight.w600,
  );
}
