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

  factory AppNotification.fromJson(Map<String, dynamic> json) => AppNotification(
    id: (json['notification_id'] as num).toInt(),
    type: json['type'] as String? ?? '',
    title: json['title'] as String? ?? '',
    message: json['message'] as String? ?? '',
    refType: json['ref_type'] as String?,
    refId: (json['ref_id'] as num?)?.toInt(),
    isRead: json['is_read'] as bool? ?? false,
    createdAt:
        DateTime.tryParse(json['created_at'] as String? ?? '') ?? DateTime.now(),
    readAt: json['read_at'] == null
        ? null
        : DateTime.tryParse(json['read_at'] as String),
  );

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
