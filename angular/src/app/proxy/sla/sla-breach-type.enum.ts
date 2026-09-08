import { mapEnumToOptions } from '@abp/ng.core';

export enum SlaBreachType {
  Response = 1,
  Resolution = 2,
}

export const slaBreachTypeOptions = mapEnumToOptions(SlaBreachType);
