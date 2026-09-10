export interface DiscordSettingsDto {
  webhookUrl: string;
  isEnabled: boolean;
  notifyOnNewTicket: boolean;
  notifyOnCriticalOnly: boolean;
  notifyOnAssigned: boolean;
  notifyOnSlaBreach: boolean;
  notifyOnResolved: boolean;
  botName: string;
  avatarUrl: string;
}

export interface UpdateDiscordSettingsDto {
  webhookUrl: string;
  isEnabled: boolean;
  notifyOnNewTicket: boolean;
  notifyOnCriticalOnly: boolean;
  notifyOnAssigned: boolean;
  notifyOnSlaBreach: boolean;
  notifyOnResolved: boolean;
  botName: string;
  avatarUrl: string;
}

export interface SendTestDiscordInput {
  webhookUrl?: string;
}

export interface TestDiscordResultDto {
  success: boolean;
  message: string;
}
