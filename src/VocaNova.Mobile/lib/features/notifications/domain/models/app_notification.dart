class AppNotification {
  const AppNotification({
    required this.id,
    required this.type,
    required this.title,
    required this.message,
    this.refType,
    this.refId,
    required this.isRead,
    required this.createdAt,
    this.readAt,
  });

  final int id;
  final String type;
  final String title;
  final String message;
  final String? refType;
  final int? refId;
  final bool isRead;
  final DateTime createdAt;
  final DateTime? readAt;

  AppNotification copyWith({bool? isRead, DateTime? readAt}) => AppNotification(
    id: id,
    type: type,
    title: title,
    message: message,
    refType: refType,
    refId: refId,
    isRead: isRead ?? this.isRead,
    createdAt: createdAt,
    readAt: readAt ?? this.readAt,
  );
}
