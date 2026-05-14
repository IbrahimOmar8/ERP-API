import 'package:flutter/material.dart';
import 'package:shimmer/shimmer.dart';

class LoadingShimmerList extends StatelessWidget {
  final int itemCount;
  final double itemHeight;

  const LoadingShimmerList({super.key, this.itemCount = 6, this.itemHeight = 72});

  @override
  Widget build(BuildContext context) {
    final base = Theme.of(context).brightness == Brightness.dark
        ? Colors.white12
        : Colors.grey.shade200;
    final highlight = Theme.of(context).brightness == Brightness.dark
        ? Colors.white24
        : Colors.grey.shade100;
    return Shimmer.fromColors(
      baseColor: base,
      highlightColor: highlight,
      child: ListView.separated(
        padding: const EdgeInsets.all(12),
        itemCount: itemCount,
        separatorBuilder: (_, __) => const SizedBox(height: 8),
        itemBuilder: (_, __) => Container(
          height: itemHeight,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
          ),
        ),
      ),
    );
  }
}

class LoadingShimmerCard extends StatelessWidget {
  final double height;
  const LoadingShimmerCard({super.key, this.height = 100});

  @override
  Widget build(BuildContext context) {
    final base = Theme.of(context).brightness == Brightness.dark
        ? Colors.white12
        : Colors.grey.shade200;
    final highlight = Theme.of(context).brightness == Brightness.dark
        ? Colors.white24
        : Colors.grey.shade100;
    return Shimmer.fromColors(
      baseColor: base,
      highlightColor: highlight,
      child: Container(
        height: height,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }
}
