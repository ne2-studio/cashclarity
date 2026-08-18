import { describe, expect, it } from 'vitest';
import { calculateLiquidityRatios } from './BucketSummarySection';

describe('liquidity ratios', () => {
  it('uses stable zero values when bank balance is zero', () => {
    expect(calculateLiquidityRatios(0, 0, 0)).toEqual({
      committedRatio: 0,
      availableRatio: 0,
      committedWidth: '0%',
      availableWidth: '0%',
    });
  });

  it('calculates ratios and clamps bar widths', () => {
    expect(calculateLiquidityRatios(100, 125, -25)).toEqual({
      committedRatio: 1.25,
      availableRatio: -0.25,
      committedWidth: '100%',
      availableWidth: '0%',
    });
  });
});
